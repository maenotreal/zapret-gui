namespace ZapretGUI;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;

    // Top bar
    private TableLayoutPanel tlpTop = null!;
    private Label lblZapretPath = null!;
    private Button btnBrowse = null!;
    private Button btnRefresh = null!;
    private Button btnLang = null!;

    // Status panel
    private GroupBox grpStatus = null!;
    private TableLayoutPanel tlpStatus = null!;
    private Label lblZapretStatus = null!;
    private Label lblWinDivertStatus = null!;
    private Label lblWinwsStatus = null!;
    private Label lblStrategyStatus = null!;

    // Service group
    private GroupBox grpService = null!;
    private Button btnInstall = null!;
    private Button btnRemove = null!;
    private Button btnStatus = null!;

    // Settings group
    private GroupBox grpSettings = null!;
    private Button btnGameFilter = null!;
    private Button btnIpSet = null!;
    private Button btnAutoUpdate = null!;

    // Updates group
    private GroupBox grpUpdates = null!;
    private Button btnUpdateIpSet = null!;
    private Button btnUpdateHosts = null!;
    private Button btnCheckUpdates = null!;

    // Tools group
    private GroupBox grpTools = null!;
    private Button btnDiagnostics = null!;
    private Button btnTests = null!;

    // Log panel
    private Panel pnlLog = null!;
    private Label lblLog = null!;
    private Button btnClearLog = null!;
    private RichTextBox rtbLog = null!;

    // Main layout
    private TableLayoutPanel tlpMain = null!;
    private TableLayoutPanel tlpGroups = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        SuspendLayout();

        // ── Form ──────────────────────────────────────────────────────────────
        Text = "Zapret Service Manager";
        Size = new Size(820, 660);
        MinimumSize = new Size(760, 580);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);

        // ── Main layout ───────────────────────────────────────────────────────
        tlpMain = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(8),
        };
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));   // path bar
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));   // status
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // groups
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 170));  // log

        // ── Path bar ──────────────────────────────────────────────────────────
        tlpTop = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 4),
        };
        tlpTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tlpTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        tlpTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        tlpTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));

        lblZapretPath = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Папка zapret не выбрана",
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(4, 0, 4, 0),
            BackColor = Color.White,
        };
        btnBrowse = MakeButton("📂 Обзор", 78);
        btnRefresh = MakeButton("↻ Статус", 86);
        btnLang = new Button
        {
            Text = "EN",
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 0, 0, 0),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
        };

        tlpTop.Controls.Add(lblZapretPath, 0, 0);
        tlpTop.Controls.Add(btnBrowse, 1, 0);
        tlpTop.Controls.Add(btnRefresh, 2, 0);
        tlpTop.Controls.Add(btnLang, 3, 0);

        // ── Status group ──────────────────────────────────────────────────────
        grpStatus = new GroupBox
        {
            Dock = DockStyle.Fill,
            Text = "Статус",
            Margin = new Padding(0, 0, 0, 4),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        };

        tlpStatus = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Padding = new Padding(4, 2, 4, 2),
        };
        for (int i = 0; i < 4; i++)
            tlpStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        lblZapretStatus = MakeStatusLabel("zapret: —");
        lblWinDivertStatus = MakeStatusLabel("WinDivert: —");
        lblWinwsStatus = MakeStatusLabel("winws.exe: —");
        lblStrategyStatus = MakeStatusLabel("Стратегия: —");

        tlpStatus.Controls.Add(lblZapretStatus, 0, 0);
        tlpStatus.Controls.Add(lblWinDivertStatus, 1, 0);
        tlpStatus.Controls.Add(lblWinwsStatus, 2, 0);
        tlpStatus.Controls.Add(lblStrategyStatus, 3, 0);
        grpStatus.Controls.Add(tlpStatus);

        // ── Groups layout ─────────────────────────────────────────────────────
        tlpGroups = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            Margin = new Padding(0),
        };
        tlpGroups.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        tlpGroups.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        tlpGroups.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4f));
        tlpGroups.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tlpGroups.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

        // Service group
        grpService = MakeGroupBox("Сервис");
        btnInstall = MakeGroupButton("⬇  Установить сервис");
        btnRemove = MakeGroupButton("✕  Удалить сервисы");
        btnStatus = MakeGroupButton("ℹ  Проверить статус");
        AddButtonsToGroup(grpService, btnInstall, btnRemove, btnStatus);

        // Settings group
        grpSettings = MakeGroupBox("Настройки");
        btnGameFilter = MakeGroupButton("🎮 Game Filter: —");
        btnIpSet = MakeGroupButton("📋 IPSet: —");
        btnAutoUpdate = MakeGroupButton("🔄 Авто-обновление: —");
        AddButtonsToGroup(grpSettings, btnGameFilter, btnIpSet, btnAutoUpdate);

        // Updates group
        grpUpdates = MakeGroupBox("Обновления");
        btnUpdateIpSet = MakeGroupButton("⬇  Обновить IPSet");
        btnUpdateHosts = MakeGroupButton("⬇  Обновить hosts");
        btnCheckUpdates = MakeGroupButton("🔍 Проверить версию");
        AddButtonsToGroup(grpUpdates, btnUpdateIpSet, btnUpdateHosts, btnCheckUpdates);

        // Tools group (spans bottom row, 3 columns)
        grpTools = MakeGroupBox("Инструменты");
        grpTools.Margin = new Padding(0);
        btnDiagnostics = MakeGroupButton("🔬 Диагностика");
        btnTests = MakeGroupButton("🧪 Запустить тесты");
        var tlpTools = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(4),
        };
        tlpTools.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tlpTools.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        btnDiagnostics.Dock = DockStyle.Fill;
        btnTests.Dock = DockStyle.Fill;
        tlpTools.Controls.Add(btnDiagnostics, 0, 0);
        tlpTools.Controls.Add(btnTests, 1, 0);
        grpTools.Controls.Add(tlpTools);

        tlpGroups.Controls.Add(grpService, 0, 0);
        tlpGroups.Controls.Add(grpSettings, 1, 0);
        tlpGroups.Controls.Add(grpUpdates, 2, 0);
        tlpGroups.SetColumnSpan(grpTools, 3);
        tlpGroups.Controls.Add(grpTools, 0, 1);

        // ── Log panel ─────────────────────────────────────────────────────────
        pnlLog = new Panel { Dock = DockStyle.Fill };

        var tlpLogHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 26,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0),
        };
        tlpLogHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tlpLogHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));

        lblLog = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Вывод:",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        };
        btnClearLog = MakeButton("Очистить", 74);

        tlpLogHeader.Controls.Add(lblLog, 0, 0);
        tlpLogHeader.Controls.Add(btnClearLog, 1, 0);

        rtbLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.FromArgb(220, 220, 220),
            Font = new Font("Consolas", 8.5f),
            BorderStyle = BorderStyle.FixedSingle,
            ScrollBars = RichTextBoxScrollBars.Vertical,
        };

        pnlLog.Controls.Add(rtbLog);
        pnlLog.Controls.Add(tlpLogHeader);

        // ── Assemble main layout ──────────────────────────────────────────────
        tlpMain.Controls.Add(tlpTop, 0, 0);
        tlpMain.Controls.Add(grpStatus, 0, 1);
        tlpMain.Controls.Add(tlpGroups, 0, 2);
        tlpMain.Controls.Add(pnlLog, 0, 3);
        Controls.Add(tlpMain);

        ResumeLayout(false);
        PerformLayout();
    }

    // ── UI factory helpers ────────────────────────────────────────────────────

    static Button MakeButton(string text, int width)
    {
        return new Button
        {
            Text = text,
            Width = width,
            Dock = DockStyle.Right,
            Height = 26,
            Margin = new Padding(4, 0, 0, 0),
        };
    }

    static Label MakeStatusLabel(string text)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9f),
            Padding = new Padding(4, 0, 0, 0),
        };
    }

    static GroupBox MakeGroupBox(string title)
    {
        return new GroupBox
        {
            Text = title,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 4, 4),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        };
    }

    static Button MakeGroupButton(string text)
    {
        return new Button
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 32,
            Margin = new Padding(0, 0, 0, 4),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Font = new Font("Segoe UI", 9f),
        };
    }

    static void AddButtonsToGroup(GroupBox grp, params Button[] buttons)
    {
        var pnl = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(4, 4, 4, 4),
            WrapContents = false,
        };
        foreach (var btn in buttons)
        {
            btn.Dock = DockStyle.None;
            btn.Width = 220;
            btn.Height = 30;
            btn.Margin = new Padding(0, 0, 0, 6);
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(8, 0, 0, 0);
            pnl.Controls.Add(btn);
        }
        grp.Controls.Add(pnl);
    }
}
