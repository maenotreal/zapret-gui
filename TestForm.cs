using System.Diagnostics;
using ZapretGUI.Localization;
using ZapretGUI.Services;

namespace ZapretGUI;

public partial class TestForm : Form
{
    readonly string _zapretDir;
    readonly List<string> _batFiles;
    CancellationTokenSource? _cts;

    static string ResultsDir =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ZapretGUI", "test results");

    public TestForm(string zapretDir)
    {
        _zapretDir = zapretDir;
        _batFiles = BatParser.FindBatFiles(zapretDir);

        InitializeComponent();
        WireEvents();
        PopulateConfigs();

        L.Changed += ApplyLocalization;
        FormClosed += (_, _) => L.Changed -= ApplyLocalization;

        if (ZapretService.QueryService("zapret") == ServiceRunStatus.Running)
            LogWarn(L.ServiceRunningWarn);

        if (!IsCurlAvailable())
            LogWarn(L.CurlMissing);
    }

    void ApplyLocalization()
    {
        Text = L.TestFormTitle;
        grpOptions.Text   = L.TestTypeGroup;
        rbStandard.Text   = L.TestStandard;
        rbDpi.Text        = L.TestDpi;
        grpConfigs.Text   = L.ConfigsGroup;
        btnSelectAll.Text  = L.SelectAll;
        btnSelectNone.Text = L.SelectNone;
        btnRun.Text        = L.RunBtn;
        btnCancel.Text     = L.StopBtn;
        btnOpenResults.Text = L.OpenResultsBtn;
        grpOutput.Text    = L.OutputGroup;
        if (lblStatus.ForeColor == Color.Gray)
            lblStatus.Text = L.TestReady;
    }

    void WireEvents()
    {
        btnSelectAll.Click += (_, _) =>
        {
            for (int i = 0; i < clbConfigs.Items.Count; i++)
                clbConfigs.SetItemChecked(i, true);
        };
        btnSelectNone.Click += (_, _) =>
        {
            for (int i = 0; i < clbConfigs.Items.Count; i++)
                clbConfigs.SetItemChecked(i, false);
        };

        rbDpi.CheckedChanged += (_, _) =>
        {
            if (rbDpi.Checked && !IsCurlAvailable())
            {
                LogWarn(L.CurlMissingForDpi);
                rbStandard.Checked = true;
            }
        };

        btnRun.Click += BtnRun_Click;
        btnCancel.Click += (_, _) =>
        {
            _cts?.Cancel();
            btnCancel.Enabled = false;
            lblStatus.Text = L.CancellingStatus;
        };
        btnOpenResults.Click += (_, _) =>
        {
            Directory.CreateDirectory(ResultsDir);
            Process.Start(new ProcessStartInfo(ResultsDir) { UseShellExecute = true });
        };
    }

    void PopulateConfigs()
    {
        clbConfigs.Items.Clear();
        foreach (var bat in _batFiles)
            clbConfigs.Items.Add(Path.GetFileNameWithoutExtension(bat), isChecked: true);

        if (clbConfigs.Items.Count == 0)
            LogWarn(L.LogNoBats);
    }

    // ── Run ───────────────────────────────────────────────────────────────────

    async void BtnRun_Click(object? s, EventArgs e)
    {
        var selectedBats = GetSelectedBats();
        if (selectedBats.Count == 0)
        {
            MessageBox.Show(L.NoSelectionMsg, L.NoSelectionTitle,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (ZapretService.QueryService("zapret") == ServiceRunStatus.Running)
        {
            var r = MessageBox.Show(L.RemoveForTestsMsg, L.ServiceActiveTitle,
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r != DialogResult.Yes) return;

            ZapretService.RemoveServices();
            LogWarn(L.ServiceRemovedForTest);
            await Task.Delay(1000);
        }

        SetRunning(true);
        rtbOutput.Clear();

        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        var progress = new Progress<TestLogEntry>(entry => AppendLog(entry));

        try
        {
            bool dpi = rbDpi.Checked;
            lblStatus.Text = L.TestRunning(dpi ? "DPI" : "Standard", selectedBats.Count);

            TestRun run;
            if (dpi)
                run = await TestService.RunDpiAsync(_zapretDir, selectedBats, progress, ct);
            else
                run = await TestService.RunStandardAsync(_zapretDir, selectedBats, progress, ct);

            if (run.BestConfig is not null)
            {
                lblStatus.Text = L.TestBestConfig(run.BestConfig);
                lblStatus.ForeColor = Color.Green;
            }
            else
            {
                lblStatus.Text = L.TestReady;
                lblStatus.ForeColor = Color.Gray;
            }

            AppendLog(new(TestLogKind.Ok, L.LogResultsSaved(ResultsDir)));
        }
        catch (OperationCanceledException)
        {
            AppendLog(new(TestLogKind.Warn, L.LogTestsCancelled));
            lblStatus.Text = L.TestCancelled;
            lblStatus.ForeColor = Color.Orange;
        }
        catch (Exception ex)
        {
            AppendLog(new(TestLogKind.Error, L.LogTestsError(ex.Message)));
            lblStatus.Text = L.TestErrorStatus;
            lblStatus.ForeColor = Color.Red;
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            SetRunning(false);
        }
    }

    List<string> GetSelectedBats()
    {
        var result = new List<string>();
        for (int i = 0; i < clbConfigs.CheckedIndices.Count; i++)
        {
            int idx = clbConfigs.CheckedIndices[i];
            if (idx < _batFiles.Count)
                result.Add(_batFiles[idx]);
        }
        return result;
    }

    void SetRunning(bool running)
    {
        btnRun.Enabled = !running;
        btnCancel.Enabled = running;
        rbStandard.Enabled = !running;
        rbDpi.Enabled = !running;
        clbConfigs.Enabled = !running;
        btnSelectAll.Enabled = !running;
        btnSelectNone.Enabled = !running;
    }

    // ── Logging ───────────────────────────────────────────────────────────────

    void AppendLog(TestLogEntry entry)
    {
        if (rtbOutput.InvokeRequired)
        {
            rtbOutput.Invoke(() => AppendLog(entry));
            return;
        }

        var color = entry.Kind switch
        {
            TestLogKind.Ok => Color.LightGreen,
            TestLogKind.Warn => Color.Yellow,
            TestLogKind.Error => Color.Salmon,
            TestLogKind.Section => Color.FromArgb(100, 180, 255),
            _ => Color.FromArgb(220, 220, 220),
        };

        string prefix = entry.Kind switch
        {
            TestLogKind.Ok => "✓ ",
            TestLogKind.Warn => "⚠ ",
            TestLogKind.Error => "✗ ",
            TestLogKind.Section => "\n══ ",
            _ => "  ",
        };
        string suffix = entry.Kind == TestLogKind.Section ? " ══" : string.Empty;

        rtbOutput.SelectionStart = rtbOutput.TextLength;
        rtbOutput.SelectionLength = 0;
        rtbOutput.SelectionColor = color;
        rtbOutput.AppendText($"{prefix}{entry.Message}{suffix}\n");
        rtbOutput.ScrollToCaret();
    }

    void LogWarn(string msg) => AppendLog(new(TestLogKind.Warn, msg));

    // ── Helpers ───────────────────────────────────────────────────────────────

    static bool IsCurlAvailable()
    {
        try
        {
            var psi = new ProcessStartInfo("curl.exe", "--version")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(2000);
            return true;
        }
        catch { return false; }
    }
}
