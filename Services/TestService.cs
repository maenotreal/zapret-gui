using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ZapretGUI.Localization;

namespace ZapretGUI.Services;

// ── Data models ───────────────────────────────────────────────────────────────

public record TestTarget(string Name, string? Url, string PingHost);

public record HttpCheckResult(string Label, string Status, int? Code, double? TimeSec);

public record TargetResult(
    string Name, bool IsUrl,
    List<HttpCheckResult> HttpResults,
    string PingResult);

public record ConfigResult(
    string ConfigName, string TestType,
    List<TargetResult> Targets,
    List<DpiTargetResult> DpiResults);

public record DpiEntry(string Id, string Provider, string Country, string Host);
public record DpiLine(string Label, string Code, long UpBytes, long DownBytes, double Time, string Status);
public record DpiTargetResult(string Id, string Provider, string Country, List<DpiLine> Lines, bool Warned);

public record TestAnalytics(int Ok, int Error, int Unsup, int PingOk, int PingFail,
    int DpiOk, int DpiFail, int DpiUnsup, int DpiBlocked);

public record TestRun(
    List<ConfigResult> Results,
    Dictionary<string, TestAnalytics> Analytics,
    string? BestConfig);

// ── Progress reporting ────────────────────────────────────────────────────────

public enum TestLogKind { Info, Ok, Warn, Error, Section }
public record TestLogEntry(TestLogKind Kind, string Message);

// ── Service ───────────────────────────────────────────────────────────────────

public static class TestService
{
    static readonly HttpClient Http = new(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 3,
    })
    { Timeout = TimeSpan.FromSeconds(8) };

    static string ResultsDir =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ZapretGUI", "test results");

    // ── Entry points ──────────────────────────────────────────────────────────

    public static async Task<TestRun> RunStandardAsync(
        string zapretDir,
        List<string> batFiles,
        IProgress<TestLogEntry> log,
        CancellationToken ct)
    {
        var targets = LoadTargets(zapretDir);
        var allResults = new List<ConfigResult>();

        log.Report(new(TestLogKind.Section, L.TsHeader("standard", batFiles.Count)));

        // Snapshot running winws to restore later
        var snapshot = GetWinwsSnapshot();

        try
        {
            for (int i = 0; i < batFiles.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                string bat = batFiles[i];
                string name = Path.GetFileNameWithoutExtension(bat);

                log.Report(new(TestLogKind.Section, L.TsConfig(i + 1, batFiles.Count, name)));

                ZapretService.KillProcess("winws");
                await WaitForProcessDeadAsync("winws", timeoutMs: 3000, ct);

                log.Report(new(TestLogKind.Info, L.TsStarting));
                using var proc = StartBat(bat, zapretDir);
                bool started = await WaitForProcessAliveAsync("winws", timeoutMs: 7000, ct);
                if (!started)
                    log.Report(new(TestLogKind.Warn, "winws не запустился — конфиг пропущен"));

                log.Report(new(TestLogKind.Info, L.TsTesting));
                var targetResults = await CheckTargetsParallelAsync(targets, log, ct);

                ZapretService.KillProcess("winws");
                proc?.Kill(entireProcessTree: true);
                await WaitForProcessDeadAsync("winws", timeoutMs: 3000, ct);

                allResults.Add(new ConfigResult(name, "standard", targetResults, []));
                await Task.Delay(500, ct);
            }
        }
        finally
        {
            ZapretService.KillProcess("winws");
            RestoreWinws(snapshot, zapretDir);
        }

        var analytics = ComputeAnalytics(allResults);
        string? best = FindBestConfig(analytics);

        log.Report(new(TestLogKind.Section, L.TsAnalytics));
        foreach (var kv in analytics)
        {
            var a = kv.Value;
            log.Report(new(TestLogKind.Info,
                $"{kv.Key}: OK={a.Ok} ERR={a.Error} UNSUP={a.Unsup} PingOK={a.PingOk} PingFail={a.PingFail}"));
        }
        if (best is not null)
            log.Report(new(TestLogKind.Ok, L.TsBest(best)));

        var run = new TestRun(allResults, analytics, best);
        await SaveResultsAsync(run);
        return run;
    }

    public static async Task<TestRun> RunDpiAsync(
        string zapretDir,
        List<string> batFiles,
        IProgress<TestLogEntry> log,
        CancellationToken ct)
    {
        log.Report(new(TestLogKind.Info, L.TsFetchingDpi));
        var dpiTargets = await FetchDpiTargetsAsync(ct);
        if (dpiTargets.Count == 0)
            log.Report(new(TestLogKind.Warn, L.TsDpiUnavail));

        // Switch ipset to "any" for accurate DPI tests
        var originalIpset = ZapretService.GetIpSetMode(zapretDir);
        string listFile = Path.Combine(zapretDir, "lists", "ipset-all.txt");
        string backupFile = listFile + ".test-backup";

        if (originalIpset != IpSetMode.Any)
        {
            log.Report(new(TestLogKind.Warn, L.TsIpSetSwitch(originalIpset.ToString())));
            try
            {
                if (File.Exists(listFile)) File.Copy(listFile, backupFile, overwrite: true);
                File.WriteAllText(listFile, string.Empty);
            }
            catch (Exception ex)
            {
                log.Report(new(TestLogKind.Warn, L.TsIpSetFail(ex.Message)));
            }
        }

        var allResults = new List<ConfigResult>();
        var snapshot = GetWinwsSnapshot();

        try
        {
            for (int i = 0; i < batFiles.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                string bat = batFiles[i];
                string name = Path.GetFileNameWithoutExtension(bat);

                log.Report(new(TestLogKind.Section, $"[{i + 1}/{batFiles.Count}] {name}"));

                ZapretService.KillProcess("winws");
                await WaitForProcessDeadAsync("winws", timeoutMs: 3000, ct);

                log.Report(new(TestLogKind.Info, L.TsStarting));
                using var proc = StartBat(bat, zapretDir);
                bool dpiStarted = await WaitForProcessAliveAsync("winws", timeoutMs: 7000, ct);
                if (!dpiStarted)
                    log.Report(new(TestLogKind.Warn, "winws не запустился — конфиг пропущен"));

                log.Report(new(TestLogKind.Info, L.TsDpiHeader(dpiTargets.Count)));
                var dpiResults = await RunDpiChecksAsync(dpiTargets, log, ct);

                ZapretService.KillProcess("winws");
                proc?.Kill(entireProcessTree: true);
                await WaitForProcessDeadAsync("winws", timeoutMs: 3000, ct);

                allResults.Add(new ConfigResult(name, "dpi", [], dpiResults));
                await Task.Delay(500, ct);
            }
        }
        finally
        {
            ZapretService.KillProcess("winws");
            RestoreWinws(snapshot, zapretDir);

            // Restore ipset
            if (originalIpset != IpSetMode.Any)
            {
                try
                {
                    if (File.Exists(backupFile))
                    {
                        File.Move(backupFile, listFile, overwrite: true);
                        log.Report(new(TestLogKind.Info, L.TsIpSetRestored));
                    }
                }
                catch (Exception ex)
                {
                    log.Report(new(TestLogKind.Warn, L.TsIpSetRestoreFail(ex.Message)));
                }
            }
        }

        var analytics = ComputeAnalytics(allResults);
        string? best = FindBestDpiConfig(analytics);

        log.Report(new(TestLogKind.Section, "АНАЛИТИКА"));
        foreach (var kv in analytics)
        {
            var a = kv.Value;
            log.Report(new(TestLogKind.Info,
                $"{kv.Key}: OK={a.DpiOk} FAIL={a.DpiFail} UNSUP={a.DpiUnsup} BLOCKED={a.DpiBlocked}"));
        }
        if (best is not null)
            log.Report(new(TestLogKind.Ok, L.TsBest(best)));

        var run = new TestRun(allResults, analytics, best);
        await SaveResultsAsync(run);
        return run;
    }

    // ── Target loading ────────────────────────────────────────────────────────

    public static List<TestTarget> LoadTargets(string zapretDir)
    {
        var targets = new List<TestTarget>();
        string targetsFile = Path.Combine(zapretDir, "utils", "targets.txt");

        if (File.Exists(targetsFile))
        {
            foreach (string line in File.ReadAllLines(targetsFile))
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith('#') || trimmed.Length == 0) continue;
                var m = Regex.Match(trimmed, @"^(.+?)\s*=\s*""(.+)""");
                if (!m.Success) continue;
                targets.Add(ParseTarget(m.Groups[1].Value.Trim(), m.Groups[2].Value.Trim()));
            }
        }

        if (targets.Count == 0)
            targets = DefaultTargets();

        return targets;
    }

    static TestTarget ParseTarget(string name, string value)
    {
        if (value.StartsWith("PING:", StringComparison.OrdinalIgnoreCase))
            return new TestTarget(name, null, value[5..].Trim());

        string pingHost = Regex.Replace(value, @"^https?://", "").Split('/')[0];
        return new TestTarget(name, value, pingHost);
    }

    static List<TestTarget> DefaultTargets() =>
    [
        ParseTarget("Discord Main",            "https://discord.com"),
        ParseTarget("Discord Gateway",         "https://gateway.discord.gg"),
        ParseTarget("Discord CDN",             "https://cdn.discordapp.com"),
        ParseTarget("Discord Updates",         "https://updates.discord.com"),
        ParseTarget("YouTube Web",             "https://www.youtube.com"),
        ParseTarget("YouTube Short",           "https://youtu.be"),
        ParseTarget("YouTube Image",           "https://i.ytimg.com"),
        ParseTarget("YouTube Video Redirect",  "https://redirector.googlevideo.com"),
        ParseTarget("Google Main",             "https://www.google.com"),
        ParseTarget("Google Gstatic",          "https://www.gstatic.com"),
        ParseTarget("Cloudflare Web",          "https://www.cloudflare.com"),
        ParseTarget("Cloudflare CDN",          "https://cdnjs.cloudflare.com"),
        ParseTarget("Cloudflare DNS 1.1.1.1",  "PING:1.1.1.1"),
        ParseTarget("Cloudflare DNS 1.0.0.1",  "PING:1.0.0.1"),
        ParseTarget("Google DNS 8.8.8.8",      "PING:8.8.8.8"),
        ParseTarget("Google DNS 8.8.4.4",      "PING:8.8.4.4"),
        ParseTarget("Quad9 DNS 9.9.9.9",       "PING:9.9.9.9"),
    ];

    // ── Standard HTTP + ping checks ───────────────────────────────────────────

    static async Task<List<TargetResult>> CheckTargetsParallelAsync(
        List<TestTarget> targets,
        IProgress<TestLogEntry> log,
        CancellationToken ct)
    {
        var tasks = targets.Select(t => CheckSingleTargetAsync(t, ct));
        var results = await Task.WhenAll(tasks);

        foreach (var r in results)
        {
            var sb = new StringBuilder($"  {r.Name.PadRight(32)}");

            if (r.IsUrl)
            {
                foreach (var h in r.HttpResults)
                {
                    var kind = h.Status switch
                    {
                        "OK" => TestLogKind.Ok,
                        "UNSUP" => TestLogKind.Warn,
                        _ => TestLogKind.Error,
                    };
                    sb.Append($" {h.Label}:{h.Status}");
                }
                sb.Append($" | Ping: {r.PingResult}");
            }
            else
            {
                sb.Append($" Ping: {r.PingResult}");
            }

            var lineKind = !r.IsUrl
                ? (r.PingResult != "Timeout" && r.PingResult != "n/a" ? TestLogKind.Ok : TestLogKind.Error)
                : r.HttpResults.All(h => h.Status == "OK") ? TestLogKind.Ok
                : r.HttpResults.Any(h => h.Status == "OK") ? TestLogKind.Warn
                : TestLogKind.Error;

            log.Report(new(lineKind, sb.ToString()));
        }

        return results.ToList();
    }

    static async Task<TargetResult> CheckSingleTargetAsync(TestTarget t, CancellationToken ct)
    {
        var httpResults = new List<HttpCheckResult>();
        string pingResult = "n/a";

        if (t.Url is not null)
        {
            var protocols = new[]
            {
                ("HTTP",   new Version(1, 1)),
                ("TLS1.2", new Version(1, 1)),
                ("TLS1.3", new Version(1, 1)),
            };

            foreach (var (label, _) in protocols)
            {
                var check = await CheckHttpAsync(t.Url, label, ct);
                httpResults.Add(check);
            }
        }

        pingResult = await PingAsync(t.PingHost, ct);

        return new TargetResult(t.Name, t.Url is not null, httpResults, pingResult);
    }

    static async Task<HttpCheckResult> CheckHttpAsync(string url, string label, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(6));

            var req = new HttpRequestMessage(HttpMethod.Head, url);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var resp = await Http.SendAsync(req, cts.Token);
            sw.Stop();

            return new HttpCheckResult(label, "OK", (int)resp.StatusCode,
                sw.Elapsed.TotalSeconds);
        }
        catch (OperationCanceledException)
        {
            return new HttpCheckResult(label, "TIMEOUT", null, null);
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("SSL") || ex.Message.Contains("certificate"))
        {
            return new HttpCheckResult(label, "SSL", null, null);
        }
        catch
        {
            return new HttpCheckResult(label, "ERROR", null, null);
        }
    }

    static async Task<string> PingAsync(string host, CancellationToken ct)
    {
        try
        {
            using var ping = new Ping();
            long total = 0;
            int success = 0;
            for (int i = 0; i < 3; i++)
            {
                ct.ThrowIfCancellationRequested();
                var reply = await ping.SendPingAsync(host, 3000);
                if (reply.Status == IPStatus.Success)
                {
                    total += reply.RoundtripTime;
                    success++;
                }
            }
            return success > 0 ? $"{total / success} ms" : "Timeout";
        }
        catch
        {
            return "Timeout";
        }
    }

    // ── DPI checks ────────────────────────────────────────────────────────────

    static async Task<List<DpiEntry>> FetchDpiTargetsAsync(CancellationToken ct)
    {
        try
        {
            const string url =
                "https://hyperion-cs.github.io/dpi-checkers/ru/tcp-16-20/suite.v2.json";
            var json = await Http.GetStringAsync(url, ct);
            var arr = JsonSerializer.Deserialize<JsonElement[]>(json)!;
            return arr.Select(e => new DpiEntry(
                e.GetProperty("id").GetString()!,
                e.GetProperty("provider").GetString()!,
                e.GetProperty("country").GetString()!,
                e.GetProperty("host").GetString()!)).ToList();
        }
        catch { return []; }
    }

    static async Task<List<DpiTargetResult>> RunDpiChecksAsync(
        List<DpiEntry> targets,
        IProgress<TestLogEntry> log,
        CancellationToken ct)
    {
        const int maxParallel = 8;
        var sem = new SemaphoreSlim(maxParallel, maxParallel);
        var tasks = targets.Select(async target =>
        {
            await sem.WaitAsync(ct);
            try { return await CheckDpiTargetAsync(target, log, ct); }
            finally { sem.Release(); }
        });

        return [.. await Task.WhenAll(tasks)];
    }

    static async Task<DpiTargetResult> CheckDpiTargetAsync(
        DpiEntry target,
        IProgress<TestLogEntry> log,
        CancellationToken ct)
    {
        var tests = new[] { ("HTTP", "--http1.1"), ("TLS1.2", "--tlsv1.2 --tls-max 1.2"), ("TLS1.3", "--tlsv1.3 --tls-max 1.3") };
        const int rangeBytes = 65536;
        const int timeoutSec = 5;

        string rangeSpec = $"0-{rangeBytes - 1}";

        // Write random payload temp file once
        string payloadFile = Path.GetTempFileName();
        try
        {
            var payload = new byte[rangeBytes];
            Random.Shared.NextBytes(payload);
            await File.WriteAllBytesAsync(payloadFile, payload, ct);
        }
        catch
        {
            return new DpiTargetResult(target.Id, target.Provider, target.Country, [], false);
        }

        var lines = new List<DpiLine>();
        bool warned = false;

        foreach (var (label, tlsFlags) in tests)
        {
            ct.ThrowIfCancellationRequested();

            string args = $"--range {rangeSpec} -m {timeoutSec} " +
                          $"-w \"%{{http_code}} %{{size_upload}} %{{size_download}} %{{time_total}}\" " +
                          $"-o NUL -X POST --data-binary @\"{payloadFile}\" -s " +
                          $"{tlsFlags} https://{target.Host}";

            var (output, exitCode) = await RunCurlAsync(args, ct);

            DpiLine line = ParseDpiOutput(label, output, exitCode, timeoutSec);
            if (line.Status == "LIKELY_BLOCKED") warned = true;
            lines.Add(line);
        }

        try { File.Delete(payloadFile); } catch { }

        var kind = warned ? TestLogKind.Warn
            : lines.All(l => l.Status is "OK" or "UNSUP") ? TestLogKind.Ok
            : TestLogKind.Error;

        log.Report(new(kind,
            $"  [{target.Country}][{target.Provider}] " +
            string.Join(" ", lines.Select(l => $"{l.Label}:{l.Status}"))));

        return new DpiTargetResult(target.Id, target.Provider, target.Country, lines, warned);
    }

    static DpiLine ParseDpiOutput(string label, string output, int exitCode, int timeoutSec)
    {
        output = output.Trim();
        var m = Regex.Match(output, @"^(\d+)\s+(\d+)\s+(\d+)\s+([\d.]+)$");

        if (m.Success)
        {
            int code = int.Parse(m.Groups[1].Value);
            long up = long.Parse(m.Groups[2].Value);
            long down = long.Parse(m.Groups[3].Value);
            double time = double.Parse(m.Groups[4].Value,
                System.Globalization.CultureInfo.InvariantCulture);

            string status = "OK";
            if (up > 0 && down == 0 && time >= timeoutSec && exitCode != 0)
                status = "LIKELY_BLOCKED";

            return new DpiLine(label, code.ToString(), up, down, time, status);
        }

        if (exitCode == 35 || output.Contains("not supported", StringComparison.OrdinalIgnoreCase)
            || output.Contains("unsupported", StringComparison.OrdinalIgnoreCase))
            return new DpiLine(label, "UNSUP", 0, 0, 0, "UNSUP");

        if (string.IsNullOrEmpty(output) || exitCode != 0)
            return new DpiLine(label, "ERR", 0, 0, 0, "FAIL");

        return new DpiLine(label, "ERR", 0, 0, 0, "FAIL");
    }

    static async Task<(string output, int exitCode)> RunCurlAsync(string args, CancellationToken ct)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("curl.exe", args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using var p = System.Diagnostics.Process.Start(psi)!;
            string output = await p.StandardOutput.ReadToEndAsync(ct);
            await p.WaitForExitAsync(ct);
            return (output, p.ExitCode);
        }
        catch { return (string.Empty, -1); }
    }

    // ── Analytics ─────────────────────────────────────────────────────────────

    static Dictionary<string, TestAnalytics> ComputeAnalytics(List<ConfigResult> results)
    {
        var dict = new Dictionary<string, TestAnalytics>();
        foreach (var r in results)
        {
            int ok = 0, err = 0, unsup = 0, pingOk = 0, pingFail = 0;
            int dpiOk = 0, dpiFail = 0, dpiUnsup = 0, dpiBlocked = 0;

            foreach (var t in r.Targets)
            {
                foreach (var h in t.HttpResults)
                {
                    if (h.Status == "OK") ok++;
                    else if (h.Status == "UNSUP") unsup++;
                    else err++;
                }
                if (t.IsUrl)
                {
                    if (t.PingResult != "Timeout" && t.PingResult != "n/a") pingOk++; else pingFail++;
                }
            }

            foreach (var d in r.DpiResults)
            {
                foreach (var l in d.Lines)
                {
                    if (l.Status == "OK") dpiOk++;
                    else if (l.Status == "UNSUP") dpiUnsup++;
                    else if (l.Status == "LIKELY_BLOCKED") dpiBlocked++;
                    else dpiFail++;
                }
            }

            dict[r.ConfigName] = new TestAnalytics(ok, err, unsup, pingOk, pingFail,
                dpiOk, dpiFail, dpiUnsup, dpiBlocked);
        }
        return dict;
    }

    static string? FindBestConfig(Dictionary<string, TestAnalytics> analytics)
    {
        return analytics
            .OrderByDescending(kv => kv.Value.Ok)
            .ThenByDescending(kv => kv.Value.PingOk)
            .FirstOrDefault().Key;
    }

    static string? FindBestDpiConfig(Dictionary<string, TestAnalytics> analytics)
    {
        return analytics
            .OrderByDescending(kv => kv.Value.DpiOk)
            .ThenByDescending(kv => kv.Value.DpiUnsup)
            .FirstOrDefault().Key;
    }

    // ── Winws snapshot / restore ──────────────────────────────────────────────

    static List<(string exe, string args)> GetWinwsSnapshot()
    {
        var result = new List<(string, string)>();
        try
        {
            foreach (var p in System.Diagnostics.Process.GetProcessesByName("winws"))
            {
                try
                {
                    string? cmdLine = GetCommandLine(p.Id);
                    if (cmdLine is null) continue;
                    string exe = p.MainModule?.FileName ?? "winws.exe";
                    string pArgs = cmdLine.StartsWith($"\"{exe}\"", StringComparison.OrdinalIgnoreCase)
                        ? cmdLine[($"\"{exe}\"".Length)..].Trim()
                        : cmdLine.StartsWith(exe, StringComparison.OrdinalIgnoreCase)
                        ? cmdLine[exe.Length..].Trim()
                        : string.Empty;
                    result.Add((exe, pArgs));
                }
                catch { }
            }
        }
        catch { }
        return result;
    }

    static void RestoreWinws(List<(string exe, string args)> snapshot, string zapretDir)
    {
        foreach (var (exe, args) in snapshot)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    WorkingDirectory = Path.GetDirectoryName(exe) ?? zapretDir,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized,
                    UseShellExecute = true,
                });
            }
            catch { }
        }
    }

    static string? GetCommandLine(int pid)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("wmic.exe",
                $"process where ProcessId={pid} get CommandLine /format:list")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };
            using var p = System.Diagnostics.Process.Start(psi)!;
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(3000);
            var m = Regex.Match(output, @"CommandLine=(.+)", RegexOptions.Multiline);
            return m.Success ? m.Groups[1].Value.Trim() : null;
        }
        catch { return null; }
    }

    // ── Process wait helpers ──────────────────────────────────────────────────

    static async Task WaitForProcessDeadAsync(string name, int timeoutMs, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (System.Diagnostics.Process.GetProcessesByName(name).Length == 0) return;
            await Task.Delay(200, ct);
        }
    }

    static async Task<bool> WaitForProcessAliveAsync(string name, int timeoutMs, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (System.Diagnostics.Process.GetProcessesByName(name).Length > 0) return true;
            await Task.Delay(300, ct);
        }
        return false;
    }

    // ── Bat launcher ─────────────────────────────────────────────────────────

    static System.Diagnostics.Process? StartBat(string batFile, string zapretDir)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{batFile}\"",
                WorkingDirectory = zapretDir,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.EnvironmentVariables["NO_UPDATE_CHECK"] = "1";
            return System.Diagnostics.Process.Start(psi);
        }
        catch { return null; }
    }

    // ── Results saving ────────────────────────────────────────────────────────

    static async Task SaveResultsAsync(TestRun run)
    {
        try
        {
            Directory.CreateDirectory(ResultsDir);
            string dateStr = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string path = Path.Combine(ResultsDir, $"test_results_{dateStr}.txt");

            var sb = new StringBuilder();
            foreach (var r in run.Results)
            {
                sb.AppendLine($"Config: {r.ConfigName} (Type: {r.TestType})");
                foreach (var t in r.Targets)
                {
                    string http = string.Join(" ", t.HttpResults.Select(h => $"{h.Label}:{h.Status}"));
                    sb.AppendLine($"  {t.Name}: {http} | Ping: {t.PingResult}");
                }
                foreach (var d in r.DpiResults)
                {
                    sb.AppendLine($"  [{d.Country}][{d.Provider}] {d.Id}");
                    foreach (var l in d.Lines)
                        sb.AppendLine($"    {l.Label}: code={l.Code} up={l.UpBytes} down={l.DownBytes} time={l.Time:F2}s status={l.Status}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("=== АНАЛИТИКА ===");
            foreach (var kv in run.Analytics)
            {
                var a = kv.Value;
                if (a.DpiOk + a.DpiFail > 0)
                    sb.AppendLine($"{kv.Key}: OK={a.DpiOk} FAIL={a.DpiFail} UNSUP={a.DpiUnsup} BLOCKED={a.DpiBlocked}");
                else
                    sb.AppendLine($"{kv.Key}: HTTP OK={a.Ok} ERR={a.Error} UNSUP={a.Unsup} PingOK={a.PingOk} PingFail={a.PingFail}");
            }
            if (run.BestConfig is not null)
                sb.AppendLine($"Лучшая стратегия: {run.BestConfig}");

            await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
        }
        catch { }
    }
}
