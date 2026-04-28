using System.Text;
using System.Text.RegularExpressions;

namespace ZapretGUI.Services;

public static class BatParser
{
    public static string? ExtractWinwsArgs(
        string batFilePath,
        string binPath,
        string listsPath,
        string gameTcp,
        string gameUdp)
    {
        string[] lines;
        try
        {
            lines = File.ReadAllLines(batFilePath, Encoding.UTF8);
        }
        catch
        {
            return null;
        }

        int startLine = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("winws.exe", StringComparison.OrdinalIgnoreCase))
            {
                startLine = i;
                break;
            }
        }

        if (startLine == -1) return null;

        var sb = new StringBuilder();
        for (int i = startLine; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            bool hasContinuation = line.EndsWith('^');
            if (hasContinuation)
                line = line[..^1].TrimEnd();

            if (i == startLine)
            {
                // Extract everything after winws.exe" (with or without closing quote)
                int idx = line.IndexOf("winws.exe\"", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                    line = line[(idx + "winws.exe\"".Length)..].Trim();
                else
                {
                    idx = line.IndexOf("winws.exe", StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                        line = line[(idx + "winws.exe".Length)..].Trim();
                    else
                        continue;
                }
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(line);
            }

            if (!hasContinuation) break;
        }

        string args = sb.ToString();
        if (string.IsNullOrWhiteSpace(args)) return null;

        string bin = binPath.TrimEnd('\\', '/');
        string lists = listsPath.TrimEnd('\\', '/');

        args = args.Replace("%BIN%", bin + "\\", StringComparison.OrdinalIgnoreCase);
        args = args.Replace("%LISTS%", lists + "\\", StringComparison.OrdinalIgnoreCase);
        args = args.Replace("%GameFilterTCP%", gameTcp, StringComparison.OrdinalIgnoreCase);
        args = args.Replace("%GameFilterUDP%", gameUdp, StringComparison.OrdinalIgnoreCase);
        args = args.Replace("%GameFilter%", gameTcp, StringComparison.OrdinalIgnoreCase);

        return args;
    }

    public static List<string> FindBatFiles(string zapretDir)
    {
        if (!Directory.Exists(zapretDir)) return [];

        return Directory
            .GetFiles(zapretDir, "*.bat", SearchOption.TopDirectoryOnly)
            .Where(f =>
            {
                string name = Path.GetFileName(f);
                return !name.StartsWith("service", StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(f => Regex.Replace(
                Path.GetFileName(f),
                @"\d+",
                m => m.Value.PadLeft(8, '0')))
            .ToList();
    }
}
