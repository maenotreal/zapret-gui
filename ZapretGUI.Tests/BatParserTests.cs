using ZapretGUI.Services;

namespace ZapretGUI.Tests;

public class BatParserTests
{
    // ── FindBatFiles ──────────────────────────────────────────────────────────

    [Fact]
    public void FindBatFiles_NonExistentDir_ReturnsEmpty()
    {
        var result = BatParser.FindBatFiles(@"C:\does_not_exist_zapret_test_xyz");
        Assert.Empty(result);
    }

    [Fact]
    public void FindBatFiles_ExcludesServiceBats()
    {
        using var tmp = new TempDir();
        File.WriteAllText(Path.Combine(tmp, "general.bat"), "");
        File.WriteAllText(Path.Combine(tmp, "general (ALT).bat"), "");
        File.WriteAllText(Path.Combine(tmp, "service.bat"), "");
        File.WriteAllText(Path.Combine(tmp, "service_install_zapret.bat"), "");

        var result = BatParser.FindBatFiles(tmp);

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, f =>
            Path.GetFileName(f).StartsWith("service", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FindBatFiles_SortsNumerically()
    {
        using var tmp = new TempDir();
        File.WriteAllText(Path.Combine(tmp, "general (ALT10).bat"), "");
        File.WriteAllText(Path.Combine(tmp, "general (ALT2).bat"), "");
        File.WriteAllText(Path.Combine(tmp, "general (ALT1).bat"), "");

        var result = BatParser.FindBatFiles(tmp);
        var names = result.Select(Path.GetFileNameWithoutExtension).ToList();

        Assert.Equal(new[] { "general (ALT1)", "general (ALT2)", "general (ALT10)" }, names);
    }

    [Fact]
    public void FindBatFiles_ReturnsOnlyTopLevelBats()
    {
        using var tmp = new TempDir();
        File.WriteAllText(Path.Combine(tmp, "general.bat"), "");
        var sub = Directory.CreateDirectory(Path.Combine(tmp, "subdir")).FullName;
        File.WriteAllText(Path.Combine(sub, "nested.bat"), "");

        var result = BatParser.FindBatFiles(tmp);

        Assert.Single(result);
    }

    // ── ExtractWinwsArgs ──────────────────────────────────────────────────────

    [Fact]
    public void ExtractWinwsArgs_NoWinwsLine_ReturnsNull()
    {
        using var tmp = new TempDir();
        var bat = Write(tmp, "test.bat", "@echo off\necho hello\n");

        var result = BatParser.ExtractWinwsArgs(bat, tmp, tmp, "", "");

        Assert.Null(result);
    }

    [Fact]
    public void ExtractWinwsArgs_SimpleLine_ExtractsArgs()
    {
        using var tmp = new TempDir();
        var bat = Write(tmp, "test.bat",
            "@echo off\r\n\"%BIN%\\winws.exe\" --wf-tcp=80,443 --dpi-desync=fake\r\n");

        var result = BatParser.ExtractWinwsArgs(bat, @"C:\zapret\bin", tmp, "", "");

        Assert.NotNull(result);
        Assert.Contains("--wf-tcp=80,443", result);
        Assert.Contains("--dpi-desync=fake", result);
    }

    [Fact]
    public void ExtractWinwsArgs_ContinuationLines_Joined()
    {
        using var tmp = new TempDir();
        var bat = Write(tmp, "test.bat",
            "@echo off\r\n\"%BIN%\\winws.exe\" --wf-tcp=80 ^\r\n--wf-udp=443\r\n");

        var result = BatParser.ExtractWinwsArgs(bat, @"C:\bin", tmp, "", "");

        Assert.NotNull(result);
        Assert.Contains("--wf-tcp=80", result);
        Assert.Contains("--wf-udp=443", result);
    }

    [Fact]
    public void ExtractWinwsArgs_ReplacesAllVariables()
    {
        using var tmp = new TempDir();
        var bat = Write(tmp, "test.bat",
            "@echo off\r\n\"%BIN%\\winws.exe\" " +
            "--filter-tcp=%GameFilterTCP% --filter-udp=%GameFilterUDP% " +
            "--ipset=%LISTS%\\list.txt\r\n");

        var result = BatParser.ExtractWinwsArgs(bat, @"C:\bin", @"C:\lists", "1024-65535", "500-1000");

        Assert.NotNull(result);
        Assert.Contains("1024-65535", result);
        Assert.Contains("500-1000", result);
        Assert.Contains(@"C:\lists\", result);
        Assert.DoesNotContain("%GameFilter", result);
        Assert.DoesNotContain("%LISTS%", result);
        Assert.DoesNotContain("%BIN%", result);
    }

    [Fact]
    public void ExtractWinwsArgs_BinPathReplacedInArgs()
    {
        using var tmp = new TempDir();
        // Parser appends trailing backslash to %BIN%, so bat uses %BIN% without extra slash
        var bat = Write(tmp, "test.bat",
            "\"%BIN%\\winws.exe\" --wf-l4=%BIN%WinDivert.dll\r\n");

        var result = BatParser.ExtractWinwsArgs(bat, @"C:\zapret\bin", tmp, "", "");

        Assert.NotNull(result);
        Assert.Contains(@"C:\zapret\bin\WinDivert.dll", result);
        Assert.DoesNotContain("%BIN%", result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static string Write(string dir, string name, string content)
    {
        var path = Path.Combine(dir, name);
        File.WriteAllText(path, content);
        return path;
    }

    sealed class TempDir : IDisposable
    {
        readonly string _path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        public TempDir() => Directory.CreateDirectory(_path);
        public static implicit operator string(TempDir t) => t._path;
        public void Dispose() { try { Directory.Delete(_path, recursive: true); } catch { } }
    }
}
