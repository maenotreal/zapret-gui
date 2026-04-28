namespace ZapretGUI.Services;

public record UpdateCheckResult(bool HasUpdate, string LocalVersion, string? RemoteVersion, string? ReleaseUrl);

public static class UpdateService
{
    const string VersionUrl =
        "https://raw.githubusercontent.com/Flowseal/zapret-discord-youtube/main/.service/version.txt";
    const string IpSetUrl =
        "https://raw.githubusercontent.com/Flowseal/zapret-discord-youtube/refs/heads/main/.service/ipset-service.txt";
    const string HostsUrl =
        "https://raw.githubusercontent.com/Flowseal/zapret-discord-youtube/refs/heads/main/.service/hosts";
    const string ReleaseBaseUrl =
        "https://github.com/Flowseal/zapret-discord-youtube/releases/tag/";
    const string LatestReleaseUrl =
        "https://github.com/Flowseal/zapret-discord-youtube/releases/latest";

    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    public static async Task<UpdateCheckResult> CheckForUpdatesAsync(string localVersion)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, VersionUrl);
            request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
            {
                NoCache = true
            };
            using var response = await Http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            string remote = (await response.Content.ReadAsStringAsync()).Trim();

            bool hasUpdate = !string.Equals(localVersion, remote, StringComparison.OrdinalIgnoreCase);
            return new UpdateCheckResult(hasUpdate, localVersion, remote,
                hasUpdate ? ReleaseBaseUrl + remote : null);
        }
        catch
        {
            return new UpdateCheckResult(false, localVersion, null, null);
        }
    }

    public static async Task<(bool ok, string message)> UpdateIpSetAsync(
        string zapretDir,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        string listFile = Path.Combine(zapretDir, "lists", "ipset-all.txt");
        try
        {
            using var response = await Http.GetAsync(IpSetUrl,
                HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            await using var file = File.Create(listFile);

            long? total = response.Content.Headers.ContentLength;
            byte[] buffer = new byte[8192];
            long read = 0;
            int bytes;

            while ((bytes = await stream.ReadAsync(buffer, ct)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, bytes), ct);
                read += bytes;
                if (total.HasValue && total > 0)
                    progress?.Report((int)(read * 100 / total));
            }

            return (true, "IPSet список успешно обновлён");
        }
        catch (OperationCanceledException)
        {
            return (false, "Отменено");
        }
        catch (Exception ex)
        {
            return (false, $"Ошибка загрузки: {ex.Message}");
        }
    }

    public static async Task<(bool needsUpdate, string? tempPath, string message)> FetchHostsAsync(
        string zapretDir,
        CancellationToken ct = default)
    {
        string hostsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "drivers", "etc", "hosts");

        try
        {
            string remote = await Http.GetStringAsync(HostsUrl, ct);
            string[] remoteLines = remote.Split('\n',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (remoteLines.Length == 0)
                return (false, null, "Не удалось получить hosts из репозитория");

            string local = File.Exists(hostsFile)
                ? File.ReadAllText(hostsFile)
                : string.Empty;

            string first = remoteLines[0];
            string last = remoteLines[^1];
            bool upToDate = local.Contains(first, StringComparison.OrdinalIgnoreCase)
                            && local.Contains(last, StringComparison.OrdinalIgnoreCase);

            if (upToDate)
                return (false, null, "Файл hosts актуален");

            // Save temp file for user to review
            string tempPath = Path.Combine(Path.GetTempPath(), "zapret_hosts_new.txt");
            await File.WriteAllTextAsync(tempPath, remote, ct);
            return (true, tempPath, "Файл hosts требует обновления");
        }
        catch (OperationCanceledException)
        {
            return (false, null, "Отменено");
        }
        catch (Exception ex)
        {
            return (false, null, $"Ошибка: {ex.Message}");
        }
    }

    public static void OpenReleasePage(string? releaseUrl = null)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = releaseUrl ?? LatestReleaseUrl,
            UseShellExecute = true,
        });
    }
}
