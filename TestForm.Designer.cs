using ZapretGUI.Localization;

namespace ZapretGUI;

partial class TestForm
{
    private System.ComponentModel.IContainer components = null!;

    private TableLayoutPanel tlpMain = null!;

    private GroupBox grpOptions = null!;
    private RadioButton rbStandard = null!;
    private RadioButton rbDpi = null!;

    private GroupBox grpConfigs = null!;
    private CheckedListBox clbConfigs = null!;
    private Button btnSelectAll = null!;
    private Button btnSelectNone = null!;

    private TableLayoutPanel tlpButtons = null!;
    private Button btnRun = null!;
    private Button btnCancel = null!;
    private Button btnOpenResults = null!;

    private GroupBox grpOutput = null!;
    private RichTextBox rtbOutput = null!;

    private Label lblStatus = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        SuspendLayout();

        Text = L.TestFormTitle;
        Size = new Size(860, 700);
        MinimumSize = new Size(700, 560);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9f);

        tlpMain = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(8),
        };
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));   // options
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));  // configs
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));   // buttons
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // output
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));   // status

        // ── Test type ─────────────────────────────────────────────────────────
        grpOptions = new GroupBox
        {
            Text = L.TestTypeGroup,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 4),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        };

        rbStandard = new RadioButton
        {
            Text = L.TestStandard,
            Checked = true,
            AutoSize = true,
            Location = new Point(12, 24),
            Font = new Font("Segoe UI", 9f),
        };
        rbDpi = new RadioButton
        {
            Text = L.TestDpi,
            AutoSize = true,
            Location = new Point(260, 24),
            Font = new Font("Segoe UI", 9f),
        };

        grpOptions.Controls.Add(rbStandard);
        grpOptions.Controls.Add(rbDpi);

        // ── Config list ───────────────────────────────────────────────────────
        grpConfigs = new GroupBox
        {
            Text = L.ConfigsGroup,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 4),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        };

        clbConfigs = new CheckedListBox
        {
            CheckOnClick = true,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9f),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Location = new Point(8, 20),
        };

        btnSelectAll = new Button
        {
            Text = L.SelectAll,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Height = 28,
            Width = 110,
        };
        btnSelectNone = new Button
        {
            Text = L.SelectNone,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Height = 28,
            Width = 110,
        };

        grpConfigs.Controls.Add(clbConfigs);
        grpConfigs.Controls.Add(btnSelectAll);
        grpConfigs.Controls.Add(btnSelectNone);

        // ── Action buttons ────────────────────────────────────────────────────
        tlpButtons = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 4),
        };
        tlpButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 114));
        tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 148));

        btnRun = new Button
        {
            Text = L.RunBtn,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Margin = new Padding(0, 0, 4, 0),
            UseVisualStyleBackColor = true,
        };

        btnCancel = new Button
        {
            Text = L.StopBtn,
            Dock = DockStyle.Fill,
            Enabled = false,
            Margin = new Padding(0, 0, 4, 0),
            UseVisualStyleBackColor = true,
        };

        btnOpenResults = new Button
        {
            Text = L.OpenResultsBtn,
            Dock = DockStyle.Fill,
            UseVisualStyleBackColor = true,
        };

        tlpButtons.Controls.Add(btnRun, 0, 0);
        tlpButtons.Controls.Add(btnCancel, 1, 0);
        tlpButtons.Controls.Add(btnOpenResults, 2, 0);

        // ── Output ────────────────────────────────────────────────────────────
        grpOutput = new GroupBox
        {
            Text = L.OutputGroup,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 4),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        };

        rtbOutput = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.FromArgb(18, 18, 18),
            ForeColor = Color.FromArgb(220, 220, 220),
            Font = new Font("Consolas", 9f),
            BorderStyle = BorderStyle.None,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Padding = new Padding(4),
        };
        grpOutput.Controls.Add(rtbOutput);

        // ── Status ────────────────────────────────────────────────────────────
        lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = L.TestReady,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.Gray,
        };

        tlpMain.Controls.Add(grpOptions, 0, 0);
        tlpMain.Controls.Add(grpConfigs, 0, 1);
        tlpMain.Controls.Add(tlpButtons, 0, 2);
        tlpMain.Controls.Add(grpOutput, 0, 3);
        tlpMain.Controls.Add(lblStatus, 0, 4);

        Controls.Add(tlpMain);
        ResumeLayout(false);
        PerformLayout();
    }

    // Layout adjustment (called after grpConfigs is sized)
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        AdjustConfigLayout();
        Resize += (_, _) => AdjustConfigLayout();
    }

    void AdjustConfigLayout()
    {
        int pad = 8;
        int btnW = btnSelectAll.Width;
        int btnH = btnSelectAll.Height;
        int gap = 6;
        int innerW = grpConfigs.ClientSize.Width - pad * 2;
        int innerH = grpConfigs.ClientSize.Height - 22; // GroupBox header

        btnSelectAll.Location  = new Point(innerW - btnW + pad, 22);
        btnSelectNone.Location = new Point(innerW - btnW + pad, 22 + btnH + gap);

        clbConfigs.Location = new Point(pad, 22);
        clbConfigs.Size = new Size(innerW - btnW - gap, innerH - 4);
    }
}
