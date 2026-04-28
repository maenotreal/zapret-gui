using System.Diagnostics;
using System.Text.Json;
using ZapretGUI.Localization;
using ZapretGUI.Services;

namespace ZapretGUI;

public partial class MainForm : Form
{
    const string LocalVersion = "1.9.7b";

    static string AppDataDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZapretGUI");
    static string SettingsFile => Path.Combine(AppDataDir, "settings.json");

    string _zapretDir = string.Empty;
    readonly System.Windows.Forms.Timer _statusTimer;

    public MainForm()
    {
        InitializeComponent();
        Icon = AppIcon.Create();

        WireEvents();
        L.Changed += ApplyLocalization;

        _statusTimer = new System.Windows.Forms.Timer { Interval = 5000 };
        _statusTimer.Tick += (_, _) => RefreshStatus();

        LoadSettings();
        ApplyLocalization();
        RefreshStatus();
        RefreshToggleLabels();
        _statusTimer.Start();
    }

    // ── Settings ──────────────────────────────────────────────────────────────

    void LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var doc = JsonDocument.Parse(File.ReadAllText(SettingsFile));
                var root = doc.RootElement;

                if (root.TryGetProperty("zapretDir", out var dir))
                    SetZapretDir(dir.GetString() ?? string.Empty);

                if (root.TryGetProperty("lang", out var lang)
                    && Enum.TryParse<Language>(lang.GetString(), out var l))
                    L.Lang = l;
            }
        }
        catch { }

        if (string.IsNullOrEmpty(_zapretDir) && IsZapretDir(AppContext.BaseDirectory))
            SetZapretDir(AppContext.BaseDirectory);
    }

    void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(AppDataDir);
            File.WriteAllText(SettingsFile,
                JsonSerializer.Serialize(new { zapretDir = _zapretDir, lang = L.Lang.ToString() }));
        }
        catch { }
    }

    void SetZapretDir(string dir)
    {
        _zapretDir = dir;
        lblZapretPath.Text = string.IsNullOrEmpty(dir) ? L.PathNotSelected : dir;
        lblZapretPath.ForeColor = IsZapretDir(dir) ? Color.Black : Color.OrangeRed;
    }

    static bool IsZapretDir(string dir) =>
        Directory.Exists(Path.Combine(dir, "bin"))
        && File.Exists(Path.Combine(dir, "bin", "winws.exe"));

    // ── Localization ──────────────────────────────────────────────────────────

    void ApplyLocalization()
    {
        Text = "Zapret Service Manager";

        // Top bar
        btnBrowse.Text  = L.BrowseBtn;
        btnRefresh.Text = L.RefreshBtn;
        btnLang.Text    = L.LangToggle;
        if (string.IsNullOrEmpty(_zapretDir))
            lblZapretPath.Text = L.PathNotSelected;

        // Groups
        grpStatus.Text   = L.StatusGroup;
        grpService.Text  = L.ServiceGroup;
        grpSettings.Text = L.SettingsGroup;
        grpUpdates.Text  = L.UpdatesGroup;
        grpTools.Text    = L.ToolsGroup;
        lblLog.Text      = L.LogLabel;
        btnClearLog.Text = L.ClearBtn;

        // Service buttons
        btnInstall.Text      = L.InstallBtn;
        btnRemove.Text       = L.RemoveBtn;
        btnStatus.Text       = L.StatusBtn;
        btnUpdateIpSet.Text  = L.UpdateIpSetBtn;
        btnUpdateHosts.Text  = L.UpdateHostsBtn;
        btnCheckUpdates.Text = L.CheckUpdatesBtn;
        btnDiagnostics.Text  = L.DiagnosticsBtn;
        btnTests.Text        = L.TestsBtn;

        // Dynamic toggle labels
        RefreshToggleLabels();
        RefreshStatus();
    }

    // ── Events ────────────────────────────────────────────────────────────────

    void WireEvents()
    {
        btnBrowse.Click   += BtnBrowse_Click;
        btnRefresh.Click  += (_, _) => { RefreshStatus(); RefreshToggleLabels(); };
        btnLang.Click     += (_, _) =>
        {
            L.Lang = L.Lang == Language.Russian ? Language.English : Language.Russian;
            SaveSettings();
        };

        btnInstall.Click      += BtnInstall_Click;
        btnRemove.Click       += BtnRemove_Click;
        btnStatus.Click       += BtnStatus_Click;
        btnGameFilter.Click   += BtnGameFilter_Click;
        btnIpSet.Click        += BtnIpSet_Click;
        btnAutoUpdate.Click   += BtnAutoUpdate_Click;
        btnUpdateIpSet.Click  += BtnUpdateIpSet_Click;
        btnUpdateHosts.Click  += BtnUpdateHosts_Click;
        btnCheckUpdates.Click += BtnCheckUpdates_Click;
        btnDiagnostics.Click  += BtnDiagnostics_Click;
        btnTests.Click        += BtnTests_Click;
        btnClearLog.Click     += (_, _) => rtbLog.Clear();
    }

    // ── Status ────────────────────────────────────────────────────────────────

    void RefreshStatus()
    {
        var s = ZapretService.GetFullStatus();

        lblZapretStatus.Text      = $"zapret: {FormatSvc(s.Zapret)}";
        lblZapretStatus.ForeColor = SvcColor(s.Zapret);

        lblWinDivertStatus.Text      = $"WinDivert: {FormatSvc(s.WinDivert)}";
        lblWinDivertStatus.ForeColor = SvcColor(s.WinDivert);

        lblWinwsStatus.Text      = s.WinwsRunning ? L.WinwsYes : L.WinwsNo;
        lblWinwsStatus.ForeColor = s.WinwsRunning ? Color.Green : Color.Red;

        lblStrategyStatus.Text      = s.InstalledStrategy is { } strat
            ? L.StrategyPrefix + strat : L.StrategyNone;
        lblStrategyStatus.ForeColor = Color.FromArgb(60, 60, 60);
    }

    void RefreshToggleLabels()
    {
        if (string.IsNullOrEmpty(_zapretDir)) return;

        var gf = ZapretService.GetGameFilter(_zapretDir);
        btnGameFilter.Text = L.GfPrefix + FormatGf(gf);

        var ipset = ZapretService.GetIpSetMode(_zapretDir);
        btnIpSet.Text = L.IpSetPrefix + FormatIpSet(ipset);

        bool autoUpd = ZapretService.GetAutoUpdateEnabled(_zapretDir);
        btnAutoUpdate.Text = L.AutoUpdatePrefix + (autoUpd ? L.AutoUpdateOn : L.AutoUpdateOff);
    }

    static string FormatSvc(ServiceRunStatus s) => s switch
    {
        ServiceRunStatus.Running      => L.SvcRunning,
        ServiceRunStatus.Stopped      => L.SvcStopped,
        ServiceRunStatus.StopPending  => L.SvcPending,
        ServiceRunStatus.NotInstalled => L.SvcNotInstalled,
        _                             => L.SvcUnknown,
    };
    static Color SvcColor(ServiceRunStatus s) => s switch
    {
        ServiceRunStatus.Running      => Color.Green,
        ServiceRunStatus.Stopped      => Color.Gray,
        ServiceRunStatus.StopPending  => Color.Orange,
        ServiceRunStatus.NotInstalled => Color.Red,
        _                             => Color.Gray,
    };
    static string FormatGf(GameFilterMode m) => m switch
    {
        GameFilterMode.Disabled  => L.GfOff,
        GameFilterMode.TcpAndUdp => L.GfTcpUdp,
        GameFilterMode.TcpOnly   => L.GfTcp,
        GameFilterMode.UdpOnly   => L.GfUdp,
        _                        => "—",
    };
    static string FormatIpSet(IpSetMode m) => m switch
    {
        IpSetMode.Any    => L.IpSetAny,
        IpSetMode.None   => L.IpSetNone,
        IpSetMode.Loaded => L.IpSetLoaded,
        _                => "—",
    };

    // ── Logging ───────────────────────────────────────────────────────────────

    void Log(string text, Color? color = null)
    {
        if (rtbLog.InvokeRequired) { rtbLog.Invoke(() => Log(text, color)); return; }
        rtbLog.SelectionStart  = rtbLog.TextLength;
        rtbLog.SelectionLength = 0;
        rtbLog.SelectionColor  = color ?? Color.FromArgb(220, 220, 220);
        rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}\n");
        rtbLog.ScrollToCaret();
    }
    void LogOk(string t)   => Log("✓ " + t, Color.LightGreen);
    void LogWarn(string t) => Log("⚠ " + t, Color.Yellow);
    void LogErr(string t)  => Log("✗ " + t, Color.Salmon);
    void LogInfo(string t) => Log("  " + t, Color.FromArgb(180, 180, 255));

    // ── Guards ────────────────────────────────────────────────────────────────

    bool RequireZapretDir()
    {
        if (!string.IsNullOrEmpty(_zapretDir) && IsZapretDir(_zapretDir)) return true;
        LogErr(L.LogNoDir);
        return false;
    }

    void SetBusy(bool busy)
    {
        foreach (var b in new Button[] {
            btnInstall, btnRemove, btnStatus,
            btnGameFilter, btnIpSet, btnAutoUpdate,
            btnUpdateIpSet, btnUpdateHosts, btnCheckUpdates,
            btnDiagnostics, btnTests, btnBrowse, btnRefresh })
            b.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    // ── Browse ────────────────────────────────────────────────────────────────

    void BtnBrowse_Click(object? s, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = L.StrategySelectLabel,
            UseDescriptionForTitle = true,
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        SetZapretDir(dlg.SelectedPath);
        SaveSettings();
        RefreshStatus();
        RefreshToggleLabels();

        if (!IsZapretDir(_zapretDir)) LogWarn(L.LogDirInvalid(_zapretDir));
        else                          LogOk(L.LogDirOk(_zapretDir));
    }

    // ── Service: Install ──────────────────────────────────────────────────────

    async void BtnInstall_Click(object? s, EventArgs e)
    {
        if (!RequireZapretDir()) return;

        var bats = BatParser.FindBatFiles(_zapretDir);
        if (bats.Count == 0) { LogErr(L.LogNoBats); return; }

        using var dlg = new StrategySelectDialog(bats);
        if (dlg.ShowDialog() != DialogResult.OK) return;

        string selectedBat  = dlg.SelectedFile;
        string strategyName = Path.GetFileNameWithoutExtension(selectedBat);

        SetBusy(true);
        Log(L.LogInstalling(strategyName));

        try
        {
            await Task.Run(() =>
            {
                string binPath   = Path.Combine(_zapretDir, "bin");
                string listsPath = Path.Combine(_zapretDir, "lists");
                string winwsPath = Path.Combine(binPath, "winws.exe");
                var gf = ZapretService.GetGameFilter(_zapretDir);
                var (tcp, udp) = ZapretService.GetGameFilterValues(gf);

                string? args = BatParser.ExtractWinwsArgs(selectedBat, binPath, listsPath, tcp, udp);
                if (args is null)
                    throw new InvalidOperationException("Could not extract winws.exe args from .bat file");

                var (ok, msg) = ZapretService.InstallService(winwsPath, args, strategyName);
                if (!ok) throw new Exception(msg);
            });

            LogOk(L.LogInstalled(strategyName));
            await Task.Delay(1000);
            RefreshStatus();
        }
        catch (Exception ex) { LogErr(ex.Message); }
        finally { SetBusy(false); }
    }

    // ── Service: Remove ───────────────────────────────────────────────────────

    async void BtnRemove_Click(object? s, EventArgs e)
    {
        if (MessageBox.Show(L.ConfirmRemove, L.ConfirmTitle,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        SetBusy(true);
        Log(L.LogRemoving);
        try
        {
            await Task.Run(ZapretService.RemoveServices);
            LogOk(L.LogRemoved);
            RefreshStatus();
        }
        catch (Exception ex) { LogErr(ex.Message); }
        finally { SetBusy(false); }
    }

    // ── Service: Status ───────────────────────────────────────────────────────

    void BtnStatus_Click(object? s, EventArgs e)
    {
        var status = ZapretService.GetFullStatus();
        Log(L.LogStatusHeader);
        Log($"zapret:     {FormatSvc(status.Zapret)}",    SvcColor(status.Zapret));
        Log($"WinDivert:  {FormatSvc(status.WinDivert)}", SvcColor(status.WinDivert));
        Log(status.WinwsRunning ? L.WinwsYes : L.WinwsNo,
            status.WinwsRunning ? Color.LightGreen : Color.Salmon);
        if (status.InstalledStrategy is { } strat)
            LogInfo(L.LogStrategyLbl + strat);
        Log(L.LogSeparator);
        RefreshStatus();
    }

    // ── Settings: Game Filter ─────────────────────────────────────────────────

    void BtnGameFilter_Click(object? s, EventArgs e)
    {
        if (!RequireZapretDir()) return;
        using var dlg = new GameFilterDialog(ZapretService.GetGameFilter(_zapretDir));
        if (dlg.ShowDialog() != DialogResult.OK) return;
        ZapretService.SetGameFilter(_zapretDir, dlg.SelectedMode);
        RefreshToggleLabels();
        LogWarn(L.LogGameFilterChanged);
    }

    // ── Settings: IPSet ───────────────────────────────────────────────────────

    void BtnIpSet_Click(object? s, EventArgs e)
    {
        if (!RequireZapretDir()) return;
        var before = ZapretService.GetIpSetMode(_zapretDir);
        ZapretService.CycleIpSetMode(_zapretDir);
        var after = ZapretService.GetIpSetMode(_zapretDir);
        RefreshToggleLabels();
        LogInfo(L.LogIpSetChanged(FormatIpSet(before), FormatIpSet(after)));
    }

    // ── Settings: Auto-Update ─────────────────────────────────────────────────

    void BtnAutoUpdate_Click(object? s, EventArgs e)
    {
        if (!RequireZapretDir()) return;
        bool current = ZapretService.GetAutoUpdateEnabled(_zapretDir);
        ZapretService.SetAutoUpdate(_zapretDir, !current);
        RefreshToggleLabels();
        LogInfo(L.LogAutoUpdate(!current ? L.AutoUpdateOn : L.AutoUpdateOff));
    }

    // ── Updates: IPSet ────────────────────────────────────────────────────────

    async void BtnUpdateIpSet_Click(object? s, EventArgs e)
    {
        if (!RequireZapretDir()) return;
        SetBusy(true);
        Log(L.LogUpdatingIpSet);
        try
        {
            var progress = new Progress<int>(p => Log(L.LogDownloading(p)));
            var (ok, msg) = await UpdateService.UpdateIpSetAsync(_zapretDir, progress);
            if (ok) LogOk(msg); else LogErr(msg);
        }
        finally { SetBusy(false); }
    }

    // ── Updates: Hosts ────────────────────────────────────────────────────────

    async void BtnUpdateHosts_Click(object? s, EventArgs e)
    {
        SetBusy(true);
        Log(L.LogCheckingHosts);
        try
        {
            var (needsUpdate, tempPath, msg) = await UpdateService.FetchHostsAsync(_zapretDir);
            if (!needsUpdate) { LogOk(msg); return; }

            LogWarn(msg);
            LogInfo(L.LogHostsNote);
            if (tempPath is not null && File.Exists(tempPath))
                Process.Start(new ProcessStartInfo("notepad.exe", tempPath) { UseShellExecute = true });

            string hostsFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "drivers", "etc", "hosts");
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{hostsFile}\"")
            { UseShellExecute = true });
        }
        catch (Exception ex) { LogErr(ex.Message); }
        finally { SetBusy(false); }
    }

    // ── Updates: Version check ────────────────────────────────────────────────

    async void BtnCheckUpdates_Click(object? s, EventArgs e)
    {
        SetBusy(true);
        Log(L.LogCheckingVersion(LocalVersion));
        try
        {
            var result = await UpdateService.CheckForUpdatesAsync(LocalVersion);
            if (result.RemoteVersion is null) { LogWarn(L.LogVersionFailed); return; }

            if (result.HasUpdate)
            {
                LogWarn(L.LogUpdateAvailable(result.RemoteVersion));
                LogInfo(L.LogReleaseUrl(result.ReleaseUrl ?? ""));

                if (MessageBox.Show(L.UpdateQuestion(result.RemoteVersion), L.UpdateTitle,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    UpdateService.OpenReleasePage(result.ReleaseUrl);
            }
            else
            {
                LogOk(L.LogNoUpdate(LocalVersion));
            }
        }
        catch (Exception ex) { LogErr(ex.Message); }
        finally { SetBusy(false); }
    }

    // ── Tools: Diagnostics ────────────────────────────────────────────────────

    async void BtnDiagnostics_Click(object? s, EventArgs e)
    {
        SetBusy(true);
        Log(L.LogDiagHeader);
        try
        {
            var progress = new Progress<DiagnosticItem>(item =>
            {
                string line = $"{item.Name}: {item.Message}";
                switch (item.Status)
                {
                    case DiagnosticStatus.Ok:      LogOk(line);   break;
                    case DiagnosticStatus.Warning: LogWarn(line); break;
                    case DiagnosticStatus.Error:   LogErr(line);  break;
                    default:                        LogInfo(line); break;
                }
                if (item.Detail is not null) LogInfo($"  → {item.Detail}");
            });

            using var cts = new CancellationTokenSource();
            await DiagnosticsService.RunAllAsync(_zapretDir, progress, cts.Token);
            Log(L.LogSeparator);
            Log(L.LogDiagDone);

            if (MessageBox.Show(L.DiagClearDiscord, L.DiagTitle,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                await Task.Run(DiagnosticsService.ClearDiscordCache);
                LogOk(L.LogDiscordCacheCleared);
            }

            var conflictNames = new[] { "GoodbyeDPI", "discordfix_zapret", "winws1", "winws2" };
            var found = conflictNames.Where(n =>
                ZapretService.QueryService(n) != ServiceRunStatus.NotInstalled).ToList();

            if (found.Count > 0)
            {
                LogWarn(L.LogConflictsFound(string.Join(", ", found)));
                if (MessageBox.Show(L.ConflictQuestion(string.Join("\n", found)), L.ConflictTitle,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    await Task.Run(DiagnosticsService.RemoveConflictingBypasses);
                    LogOk(L.LogConflictsRemoved);
                }
            }
        }
        catch (OperationCanceledException) { LogWarn(L.LogDiagCancelled); }
        catch (Exception ex)               { LogErr(ex.Message); }
        finally { SetBusy(false); RefreshStatus(); }
    }

    // ── Tools: Tests ──────────────────────────────────────────────────────────

    void BtnTests_Click(object? s, EventArgs e)
    {
        if (!RequireZapretDir()) return;
        using var form = new TestForm(_zapretDir);
        form.ShowDialog(this);
    }
}

// ── Strategy selection dialog ─────────────────────────────────────────────────

file sealed class StrategySelectDialog : Form
{
    readonly ListBox _list;
    readonly List<string> _files;
    public string SelectedFile => _files[_list.SelectedIndex];

    public StrategySelectDialog(List<string> batFiles)
    {
        _files = batFiles;
        Text = L.StrategySelectTitle;
        Size = new Size(480, 360);
        MinimumSize = new Size(360, 280);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9f);

        var lbl = new Label
        {
            Dock = DockStyle.Top, Height = 26,
            Text = L.StrategySelectLabel,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(4, 0, 0, 0),
        };

        _list = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
        foreach (var f in batFiles)
            _list.Items.Add(Path.GetFileNameWithoutExtension(f));
        if (_list.Items.Count > 0) _list.SelectedIndex = 0;
        _list.DoubleClick += (_, _) => DialogResult = DialogResult.OK;

        var btnOk     = new Button { Text = L.InstallBtnDlg, DialogResult = DialogResult.OK,     Width = 100 };
        var btnCancel = new Button { Text = L.CancelBtnDlg,  DialogResult = DialogResult.Cancel,  Width = 80  };
        AcceptButton = btnOk; CancelButton = btnCancel;

        var pnlBtn = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft,
            Height = 40, Padding = new Padding(4),
        };
        pnlBtn.Controls.Add(btnCancel);
        pnlBtn.Controls.Add(btnOk);

        Controls.Add(_list);
        Controls.Add(pnlBtn);
        Controls.Add(lbl);
    }
}

// ── Game filter dialog ────────────────────────────────────────────────────────

file sealed class GameFilterDialog : Form
{
    public GameFilterMode SelectedMode { get; private set; }

    public GameFilterDialog(GameFilterMode current)
    {
        SelectedMode = current;
        Text = L.GameFilterTitle;
        Size = new Size(320, 220);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9f);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var options = new[]
        {
            (GameFilterMode.Disabled,  L.GfOptionOff),
            (GameFilterMode.TcpAndUdp, L.GfOptionTcpUdp),
            (GameFilterMode.TcpOnly,   L.GfOptionTcp),
            (GameFilterMode.UdpOnly,   L.GfOptionUdp),
        };

        var pnl = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(12),
        };
        foreach (var (mode, label) in options)
        {
            var rb = new RadioButton
            {
                Text = label, Tag = mode, AutoSize = true,
                Checked = mode == current,
                Margin = new Padding(0, 4, 0, 4),
            };
            pnl.Controls.Add(rb);
        }

        var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 80 };
        btnOk.Click += (_, _) =>
        {
            foreach (RadioButton rb in pnl.Controls.OfType<RadioButton>())
                if (rb.Checked) SelectedMode = (GameFilterMode)rb.Tag!;
        };
        var btnCancel = new Button { Text = L.CancelBtnDlg, DialogResult = DialogResult.Cancel, Width = 80 };
        AcceptButton = btnOk; CancelButton = btnCancel;

        var pnlBtn = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft,
            Height = 40, Padding = new Padding(4),
        };
        pnlBtn.Controls.Add(btnCancel);
        pnlBtn.Controls.Add(btnOk);

        Controls.Add(pnl);
        Controls.Add(pnlBtn);
    }
}
