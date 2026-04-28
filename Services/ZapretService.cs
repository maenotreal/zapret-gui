using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using Microsoft.Win32;

namespace ZapretGUI.Services;

public enum ServiceRunStatus { NotInstalled, Stopped, Running, StopPending, Unknown }
public enum GameFilterMode { Disabled, TcpAndUdp, TcpOnly, UdpOnly }
public enum IpSetMode { Any, None, Loaded }

public class ServiceStatusInfo
{
    public ServiceRunStatus Zapret { get; init; }
    public ServiceRunStatus WinDivert { get; init; }
    public bool WinwsRunning { get; init; }
    public string? InstalledStrategy { get; init; }
}

public static class ZapretService
{
    // ── Service queries ──────────────────────────────────────────────────────

    public static ServiceRunStatus QueryService(string name)
    {
        try
        {
            using var sc = new ServiceController(name);
            return sc.Status switch
            {
                ServiceControllerStatus.Running => ServiceRunStatus.Running,
                ServiceControllerStatus.StopPending => ServiceRunStatus.StopPending,
                ServiceControllerStatus.Stopped => ServiceRunStatus.Stopped,
                _ => ServiceRunStatus.Unknown,
            };
        }
        catch (InvalidOperationException)
        {
            return ServiceRunStatus.NotInstalled;
        }
    }

    public static bool IsWinwsRunning()
    {
        return Process.GetProcessesByName("winws").Length > 0;
    }

    public static string? GetInstalledStrategy()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"System\CurrentControlSet\Services\zapret");
            return key?.GetValue("zapret-discord-youtube") as string;
        }
        catch { return null; }
    }

    public static ServiceStatusInfo GetFullStatus()
    {
        return new ServiceStatusInfo
        {
            Zapret = QueryService("zapret"),
            WinDivert = QueryService("WinDivert"),
            WinwsRunning = IsWinwsRunning(),
            InstalledStrategy = GetInstalledStrategy(),
        };
    }

    // ── Service install/remove ───────────────────────────────────────────────

    public static (bool ok, string message) InstallService(
        string winwsPath, string args, string strategyName)
    {
        RemoveService("zapret");
        EnableTcpTimestamps();

        string imagePath = $"\"{winwsPath}\" {args}";
        bool created = NativeMethods.CreateWindowsService("zapret", "zapret", imagePath);
        if (!created)
            return (false, $"Не удалось создать сервис (код ошибки: {Marshal.GetLastWin32Error()})");

        SetServiceDescription("zapret", "Zapret DPI bypass software");
        StartService("zapret");

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"System\CurrentControlSet\Services\zapret", writable: true);
            key?.SetValue("zapret-discord-youtube", strategyName, RegistryValueKind.String);
        }
        catch { }

        return (true, $"Сервис запущен со стратегией: {strategyName}");
    }

    public static void RemoveServices()
    {
        KillProcess("winws");
        RemoveService("zapret");
        RemoveService("WinDivert");
        RemoveService("WinDivert14");
    }

    public static void RemoveService(string name)
    {
        try
        {
            using var sc = new ServiceController(name);
            if (sc.Status == ServiceControllerStatus.Running)
            {
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5));
            }
        }
        catch { }

        RunSc($"delete {name}");
    }

    public static void StartService(string name)
    {
        try
        {
            using var sc = new ServiceController(name);
            if (sc.Status != ServiceControllerStatus.Running)
                sc.Start();
        }
        catch { }
    }

    // ── Toggles & settings ───────────────────────────────────────────────────

    public static GameFilterMode GetGameFilter(string zapretDir)
    {
        string flag = Path.Combine(zapretDir, "utils", "game_filter.enabled");
        if (!File.Exists(flag)) return GameFilterMode.Disabled;

        string? mode = File.ReadLines(flag).FirstOrDefault()?.Trim().ToLower();
        return mode switch
        {
            "all" => GameFilterMode.TcpAndUdp,
            "tcp" => GameFilterMode.TcpOnly,
            "udp" => GameFilterMode.UdpOnly,
            _ => GameFilterMode.TcpAndUdp,
        };
    }

    public static void SetGameFilter(string zapretDir, GameFilterMode mode)
    {
        string flag = Path.Combine(zapretDir, "utils", "game_filter.enabled");
        if (mode == GameFilterMode.Disabled)
        {
            if (File.Exists(flag)) File.Delete(flag);
            return;
        }
        string content = mode switch
        {
            GameFilterMode.TcpAndUdp => "all",
            GameFilterMode.TcpOnly => "tcp",
            GameFilterMode.UdpOnly => "udp",
            _ => "all",
        };
        Directory.CreateDirectory(Path.GetDirectoryName(flag)!);
        File.WriteAllText(flag, content);
    }

    public static (string tcp, string udp) GetGameFilterValues(GameFilterMode mode)
    {
        return mode switch
        {
            GameFilterMode.Disabled => ("12", "12"),
            GameFilterMode.TcpAndUdp => ("1024-65535", "1024-65535"),
            GameFilterMode.TcpOnly => ("1024-65535", "12"),
            GameFilterMode.UdpOnly => ("12", "1024-65535"),
            _ => ("12", "12"),
        };
    }

    public static IpSetMode GetIpSetMode(string zapretDir)
    {
        string listFile = Path.Combine(zapretDir, "lists", "ipset-all.txt");
        if (!File.Exists(listFile)) return IpSetMode.Any;

        string content = File.ReadAllText(listFile).Trim();
        if (string.IsNullOrEmpty(content)) return IpSetMode.Any;
        if (content.Contains("203.0.113.113/32")) return IpSetMode.None;
        return IpSetMode.Loaded;
    }

    public static void CycleIpSetMode(string zapretDir)
    {
        string listFile = Path.Combine(zapretDir, "lists", "ipset-all.txt");
        string backupFile = listFile + ".backup";
        IpSetMode current = GetIpSetMode(zapretDir);

        if (current == IpSetMode.Loaded)
        {
            if (File.Exists(backupFile)) File.Delete(backupFile);
            File.Move(listFile, backupFile);
            File.WriteAllText(listFile, "203.0.113.113/32\r\n");
        }
        else if (current == IpSetMode.None)
        {
            File.WriteAllText(listFile, string.Empty);
        }
        else // Any
        {
            if (File.Exists(backupFile))
            {
                File.Delete(listFile);
                File.Move(backupFile, listFile);
            }
        }
    }

    public static bool GetAutoUpdateEnabled(string zapretDir)
    {
        return File.Exists(Path.Combine(zapretDir, "utils", "check_updates.enabled"));
    }

    public static void SetAutoUpdate(string zapretDir, bool enabled)
    {
        string flag = Path.Combine(zapretDir, "utils", "check_updates.enabled");
        Directory.CreateDirectory(Path.GetDirectoryName(flag)!);
        if (enabled)
            File.WriteAllText(flag, "ENABLED");
        else if (File.Exists(flag))
            File.Delete(flag);
    }

    // ── Tools ────────────────────────────────────────────────────────────────

    public static void EnableTcpTimestamps()
    {
        RunNetsh("interface tcp set global timestamps=enabled");
    }

    public static void RunTests(string zapretDir)
    {
        string ps1 = Path.Combine(zapretDir, "utils", "test zapret.ps1");
        if (!File.Exists(ps1)) return;

        Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{ps1}\"",
            UseShellExecute = true,
        });
    }

    public static void KillProcess(string name)
    {
        foreach (var p in Process.GetProcessesByName(name))
        {
            try { p.Kill(); } catch { }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static void SetServiceDescription(string name, string description)
    {
        RunSc($"description {name} \"{description}\"");
    }

    static void RunSc(string args)
    {
        try
        {
            var psi = new ProcessStartInfo("sc.exe", args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(5000);
        }
        catch { }
    }

    static void RunNetsh(string args)
    {
        try
        {
            var psi = new ProcessStartInfo("netsh.exe", args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(3000);
        }
        catch { }
    }
}

// ── P/Invoke for CreateService ───────────────────────────────────────────────
file static class NativeMethods
{
    const uint SC_MANAGER_CREATE_SERVICE = 0x0002;
    const uint SC_MANAGER_ALL_ACCESS = 0xF003F;
    const uint SERVICE_WIN32_OWN_PROCESS = 0x00000010;
    const uint SERVICE_AUTO_START = 0x00000002;
    const uint SERVICE_ERROR_NORMAL = 0x00000001;
    const uint SERVICE_ALL_ACCESS = 0xF01FF;
    const uint DELETE = 0x00010000;

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr OpenSCManager(string? machine, string? database, uint access);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr CreateService(
        IntPtr hSCManager, string serviceName, string displayName,
        uint dwDesiredAccess, uint dwServiceType, uint dwStartType,
        uint dwErrorControl, string binaryPathName,
        string? loadOrderGroup, IntPtr tagId,
        string? dependencies, string? serviceStartName, string? password);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool CloseServiceHandle(IntPtr hSCObject);

    public static bool CreateWindowsService(string name, string displayName, string imagePath)
    {
        IntPtr scm = OpenSCManager(null, null, SC_MANAGER_CREATE_SERVICE);
        if (scm == IntPtr.Zero) return false;

        try
        {
            IntPtr svc = CreateService(
                scm, name, displayName,
                SERVICE_ALL_ACCESS,
                SERVICE_WIN32_OWN_PROCESS,
                SERVICE_AUTO_START,
                SERVICE_ERROR_NORMAL,
                imagePath,
                null, IntPtr.Zero, null, null, null);

            if (svc == IntPtr.Zero) return false;
            CloseServiceHandle(svc);
            return true;
        }
        finally
        {
            CloseServiceHandle(scm);
        }
    }
}
