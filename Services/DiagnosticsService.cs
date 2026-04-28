using System.Diagnostics;
using Microsoft.Win32;
using ZapretGUI.Localization;

namespace ZapretGUI.Services;

public record DiagnosticItem(string Name, DiagnosticStatus Status, string Message, string? Detail = null);
public enum DiagnosticStatus { Ok, Warning, Error, Info }

public static class DiagnosticsService
{
    public static async Task<List<DiagnosticItem>> RunAllAsync(
        string zapretDir,
        IProgress<DiagnosticItem>? progress = null,
        CancellationToken ct = default)
    {
        var results = new List<DiagnosticItem>();

        var checks = new Func<string, Task<DiagnosticItem>>[]
        {
            CheckBaseFilteringEngine,
            CheckProxy,
            CheckTcpTimestamps,
            CheckAdguard,
            CheckKiller,
            CheckIntelConnectivity,
            CheckCheckPoint,
            CheckSmartByte,
            CheckVpn,
            CheckDns,
            CheckHostsFile,
            _ => CheckWinDivertConflict(zapretDir, _),
            _ => CheckConflictingBypasses(zapretDir, _),
            _ => CheckWinDivertSysFile(zapretDir, _),
        };

        foreach (var check in checks)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var item = await Task.Run(() => check(zapretDir), ct);
                results.Add(item);
                progress?.Report(item);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                var item = new DiagnosticItem(L.DiagErrorName, DiagnosticStatus.Error, ex.Message);
                results.Add(item);
                progress?.Report(item);
            }
        }

        return results;
    }

    static Task<DiagnosticItem> CheckBaseFilteringEngine(string _)
    {
        var status = ZapretService.QueryService("BFE");
        return Task.FromResult(status == ServiceRunStatus.Running
            ? new DiagnosticItem(L.DiagBfeName, DiagnosticStatus.Ok, L.DiagBfeOk)
            : new DiagnosticItem(L.DiagBfeName, DiagnosticStatus.Error, L.DiagBfeErr));
    }

    static Task<DiagnosticItem> CheckProxy(string _)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Internet Settings");
            var enabled = key?.GetValue("ProxyEnable");
            if (enabled is int val && val == 1)
            {
                var server = key?.GetValue("ProxyServer") as string ?? "unknown";
                return Task.FromResult(new DiagnosticItem(
                    L.DiagProxyName, DiagnosticStatus.Warning,
                    L.DiagProxyWarn(server), L.DiagProxyDetail));
            }
        }
        catch { }
        return Task.FromResult(new DiagnosticItem(L.DiagProxyName, DiagnosticStatus.Ok, L.DiagProxyOk));
    }

    static Task<DiagnosticItem> CheckTcpTimestamps(string _)
    {
        try
        {
            var output = RunCommand("netsh.exe", "interface tcp show global");
            bool enabled = output.Contains("timestamp", StringComparison.OrdinalIgnoreCase)
                           && output.Contains("enabled", StringComparison.OrdinalIgnoreCase);
            if (enabled)
                return Task.FromResult(new DiagnosticItem(L.DiagTcpName, DiagnosticStatus.Ok, L.DiagTcpOk));

            ZapretService.EnableTcpTimestamps();
            return Task.FromResult(new DiagnosticItem(L.DiagTcpName, DiagnosticStatus.Warning, L.DiagTcpFixed));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new DiagnosticItem(L.DiagTcpName, DiagnosticStatus.Error, ex.Message));
        }
    }

    static Task<DiagnosticItem> CheckAdguard(string _)
    {
        bool found = Process.GetProcessesByName("AdguardSvc").Length > 0;
        return Task.FromResult(found
            ? new DiagnosticItem(L.DiagAdguardName, DiagnosticStatus.Error, L.DiagAdguardErr)
            : new DiagnosticItem(L.DiagAdguardName, DiagnosticStatus.Ok, L.DiagAdguardOk));
    }

    static Task<DiagnosticItem> CheckKiller(string _)
    {
        var output = RunCommand("sc.exe", "query");
        bool found = output.Contains("Killer", StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(found
            ? new DiagnosticItem(L.DiagKillerName, DiagnosticStatus.Error, L.DiagKillerErr)
            : new DiagnosticItem(L.DiagKillerName, DiagnosticStatus.Ok, L.DiagKillerOk));
    }

    static Task<DiagnosticItem> CheckIntelConnectivity(string _)
    {
        var output = RunCommand("sc.exe", "query");
        bool found = output.Contains("Intel", StringComparison.OrdinalIgnoreCase)
                     && output.Contains("Connectivity", StringComparison.OrdinalIgnoreCase)
                     && output.Contains("Network", StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(found
            ? new DiagnosticItem(L.DiagIntelName, DiagnosticStatus.Error, L.DiagIntelErr)
            : new DiagnosticItem(L.DiagIntelName, DiagnosticStatus.Ok, L.DiagIntelOk));
    }

    static Task<DiagnosticItem> CheckCheckPoint(string _)
    {
        var output = RunCommand("sc.exe", "query");
        bool found = output.Contains("TracSrvWrapper", StringComparison.OrdinalIgnoreCase)
                     || output.Contains("EPWD", StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(found
            ? new DiagnosticItem(L.DiagCpName, DiagnosticStatus.Error, L.DiagCpErr)
            : new DiagnosticItem(L.DiagCpName, DiagnosticStatus.Ok, L.DiagCpOk));
    }

    static Task<DiagnosticItem> CheckSmartByte(string _)
    {
        var output = RunCommand("sc.exe", "query");
        bool found = output.Contains("SmartByte", StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(found
            ? new DiagnosticItem(L.DiagSbName, DiagnosticStatus.Error, L.DiagSbErr)
            : new DiagnosticItem(L.DiagSbName, DiagnosticStatus.Ok, L.DiagSbOk));
    }

    static Task<DiagnosticItem> CheckVpn(string _)
    {
        var output = RunCommand("sc.exe", "query");
        var vpnNames = output.Split('\n')
            .Where(l => l.Contains("VPN", StringComparison.OrdinalIgnoreCase)
                        && l.Contains("SERVICE_NAME", StringComparison.OrdinalIgnoreCase))
            .Select(l => l.Replace("SERVICE_NAME:", "").Trim())
            .ToList();

        return Task.FromResult(vpnNames.Count > 0
            ? new DiagnosticItem(L.DiagVpnName, DiagnosticStatus.Warning,
                L.DiagVpnWarn(string.Join(", ", vpnNames)), L.DiagVpnDetail)
            : new DiagnosticItem(L.DiagVpnName, DiagnosticStatus.Ok, L.DiagVpnOk));
    }

    static Task<DiagnosticItem> CheckDns(string _)
    {
        try
        {
            var output = RunCommand("powershell.exe",
                "-NoProfile -Command \"Get-ChildItem -Recurse -Path 'HKLM:System\\CurrentControlSet\\Services\\Dnscache\\InterfaceSpecificParameters\\' | Get-ItemProperty | Where-Object { $_.DohFlags -gt 0 } | Measure-Object | Select-Object -ExpandProperty Count\"");
            if (int.TryParse(output.Trim(), out int count) && count > 0)
                return Task.FromResult(new DiagnosticItem(L.DiagDnsName, DiagnosticStatus.Ok, L.DiagDnsOk));
        }
        catch { }

        return Task.FromResult(new DiagnosticItem(L.DiagDnsName, DiagnosticStatus.Warning,
            L.DiagDnsWarn, L.DiagDnsDetail));
    }

    static Task<DiagnosticItem> CheckHostsFile(string _)
    {
        try
        {
            string hosts = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "drivers", "etc", "hosts");
            if (File.Exists(hosts))
            {
                string content = File.ReadAllText(hosts);
                if (content.Contains("youtube.com", StringComparison.OrdinalIgnoreCase)
                    || content.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult(new DiagnosticItem(
                        L.DiagHostsName, DiagnosticStatus.Warning, L.DiagHostsWarn));
            }
        }
        catch { }
        return Task.FromResult(new DiagnosticItem(L.DiagHostsName, DiagnosticStatus.Ok, L.DiagHostsOk));
    }

    static Task<DiagnosticItem> CheckWinDivertConflict(string zapretDir, string _)
    {
        bool winwsRunning = ZapretService.IsWinwsRunning();
        var winDivertStatus = ZapretService.QueryService("WinDivert");

        if (!winwsRunning && winDivertStatus == ServiceRunStatus.Running)
        {
            ZapretService.RemoveService("WinDivert");
            return Task.FromResult(new DiagnosticItem(
                L.DiagWdName, DiagnosticStatus.Warning, L.DiagWdFixed));
        }

        return Task.FromResult(new DiagnosticItem(L.DiagWdName, DiagnosticStatus.Ok, L.DiagWdOk));
    }

    static Task<DiagnosticItem> CheckConflictingBypasses(string zapretDir, string _)
    {
        var conflicting = new[] { "GoodbyeDPI", "discordfix_zapret", "winws1", "winws2" };
        var found = conflicting.Where(s =>
            ZapretService.QueryService(s) != ServiceRunStatus.NotInstalled).ToList();

        return Task.FromResult(found.Count > 0
            ? new DiagnosticItem(L.DiagCflName, DiagnosticStatus.Error,
                L.DiagCflErr(string.Join(", ", found)), L.DiagCflDetail)
            : new DiagnosticItem(L.DiagCflName, DiagnosticStatus.Ok, L.DiagCflOk));
    }

    static Task<DiagnosticItem> CheckWinDivertSysFile(string zapretDir, string _)
    {
        string binPath = Path.Combine(zapretDir, "bin");
        if (!Directory.Exists(binPath) || !Directory.GetFiles(binPath, "*.sys").Any())
            return Task.FromResult(new DiagnosticItem(
                L.DiagSysFileName, DiagnosticStatus.Error, L.DiagSysFileErr));

        return Task.FromResult(new DiagnosticItem(
            L.DiagSysFileName, DiagnosticStatus.Ok, L.DiagSysFileOk));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    public static string RunCommand(string exe, string args)
    {
        try
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi)!;
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);
            return output;
        }
        catch { return string.Empty; }
    }

    public static void ClearDiscordCache()
    {
        ZapretService.KillProcess("Discord");
        string discordDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord");
        foreach (var folder in new[] { "Cache", "Code Cache", "GPUCache" })
        {
            string path = Path.Combine(discordDir, folder);
            if (Directory.Exists(path))
                try { Directory.Delete(path, recursive: true); } catch { }
        }
    }

    public static void RemoveConflictingBypasses()
    {
        var conflicting = new[] { "GoodbyeDPI", "discordfix_zapret", "winws1", "winws2" };
        foreach (var name in conflicting)
        {
            if (ZapretService.QueryService(name) != ServiceRunStatus.NotInstalled)
                ZapretService.RemoveService(name);
        }
        ZapretService.RemoveService("WinDivert");
        ZapretService.RemoveService("WinDivert14");
    }
}
