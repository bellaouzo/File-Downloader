namespace FileDownloader.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel pnlTop;
    private Panel            pnlSeparator;
    private Panel            pnlBottom;
    private Label            lblUrlInput;
    private TextBox          txtUrl;
    private Button           btnDownload;
    private Label            lblOutputDir;
    private TextBox          txtOutputDir;
    private Button           btnBrowse;
    private ListView         lvDownloads;
    private ColumnHeader     colFileName;
    private ColumnHeader     colStatus;
    private ColumnHeader     colProgress;
    private ColumnHeader     colSpeed;
    private ColumnHeader     colSize;
    private ColumnHeader     colUrl;
    private FlowLayoutPanel  pnlActions;
    private Button           btnCancelAll;
    private Button           btnRemoveSelected;
    private Button           btnClearDone;
    private ProgressBar      progressOverall;
    private Label            lblStatus;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        // --- Top panel ---
        pnlTop = new TableLayoutPanel
        {
            Dock        = DockStyle.Top,
            Height      = 114,
            Padding     = new Padding(12, 8, 12, 6),
            BackColor   = Color.FromArgb(245, 245, 247),
            ColumnCount = 3,
            RowCount    = 2
        };
        pnlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        pnlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100));
        pnlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
        pnlTop.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        pnlTop.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        lblOutputDir = new Label
        {
            Text      = "Save folder:",
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font      = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(50, 50, 50)
        };
        lblUrlInput = new Label
        {
            Text      = "URL(s):",
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            Padding   = new Padding(0, 6, 0, 0),
            Font      = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(50, 50, 50)
        };

        txtOutputDir = new TextBox
        {
            Dock            = DockStyle.Fill,
            PlaceholderText = "Choose where to save files..."
        };
        txtUrl = new TextBox
        {
            Dock            = DockStyle.Fill,
            Multiline       = true,
            AcceptsReturn   = true,
            ScrollBars      = ScrollBars.Vertical,
            PlaceholderText = "Paste one URL per line, then hit Download (or Ctrl+Enter)"
        };

        btnBrowse = new Button
        {
            Text   = "Browse",
            Dock   = DockStyle.Fill,
            Margin = new Padding(6, 2, 0, 2)
        };

        btnDownload = new Button
        {
            Text      = "Download",
            Dock      = DockStyle.Top,
            Height    = 30,
            Margin    = new Padding(6, 2, 0, 2),
            BackColor = Color.FromArgb(30, 120, 220),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        btnDownload.FlatAppearance.BorderSize = 0;

        pnlTop.Controls.Add(lblOutputDir, 0, 0);
        pnlTop.Controls.Add(txtOutputDir, 1, 0);
        pnlTop.Controls.Add(btnBrowse,    2, 0);
        pnlTop.Controls.Add(lblUrlInput,  0, 1);
        pnlTop.Controls.Add(txtUrl,       1, 1);
        pnlTop.Controls.Add(btnDownload,  2, 1);

        // Separator line
        pnlSeparator = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 1,
            BackColor = Color.FromArgb(210, 210, 215)
        };

        // --- Download list ---
        colFileName = new ColumnHeader { Text = "Filename",    Width = 185 };
        colStatus   = new ColumnHeader { Text = "Status",     Width = 95  };
        colProgress = new ColumnHeader { Text = "%",          Width = 55  };
        colSpeed    = new ColumnHeader { Text = "Speed",      Width = 90  };
        colSize     = new ColumnHeader { Text = "Size",       Width = 80  };
        colUrl      = new ColumnHeader { Text = "URL",        Width = 355 };

        lvDownloads = new ListView
        {
            Dock          = DockStyle.Fill,
            View          = View.Details,
            FullRowSelect = true,
            GridLines     = true,
            MultiSelect   = true,
            Font          = new Font("Segoe UI", 9f),
            BorderStyle   = BorderStyle.None
        };
        lvDownloads.Columns.AddRange(new[] { colFileName, colStatus, colProgress, colSpeed, colSize, colUrl });

        // --- Bottom panel ---
        pnlBottom = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 74,
            BackColor = Color.FromArgb(245, 245, 247)
        };

        pnlActions = new FlowLayoutPanel
        {
            Dock          = DockStyle.Top,
            Height        = 38,
            Padding       = new Padding(8, 5, 0, 0),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false
        };

        Padding btnMargin = new Padding(0, 0, 6, 0);

        btnCancelAll = new Button
        {
            Text      = "Stop All",
            Width     = 90,
            Height    = 27,
            Margin    = btnMargin,
            Enabled   = false,
            FlatStyle = FlatStyle.Flat,
            BackColor = SystemColors.Control
        };
        btnCancelAll.FlatAppearance.BorderColor = Color.Silver;

        btnRemoveSelected = new Button { Text = "Remove", Width = 80, Height = 27, Margin = btnMargin };

        btnClearDone = new Button { Text = "Clear Finished", Width = 105, Height = 27, Margin = new Padding(0) };

        pnlActions.Controls.AddRange(new Control[] { btnCancelAll, btnRemoveSelected, btnClearDone });

        progressOverall = new ProgressBar
        {
            Dock    = DockStyle.Top,
            Height  = 14,
            Minimum = 0,
            Maximum = 100,
            Value   = 0,
            Style   = ProgressBarStyle.Continuous
        };

        lblStatus = new Label
        {
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(10, 0, 0, 0),
            Text      = "Ready",
            ForeColor = Color.DimGray,
            Font      = new Font("Segoe UI", 8.5f)
        };

        pnlBottom.Controls.Add(lblStatus);
        pnlBottom.Controls.Add(progressOverall);
        pnlBottom.Controls.Add(pnlActions);

        // --- Form ---
        Text          = "File Downloader";
        Size          = new Size(900, 580);
        MinimumSize   = new Size(720, 420);
        StartPosition = FormStartPosition.CenterScreen;
        Font          = new Font("Segoe UI", 9f);
        BackColor     = Color.White;

        Controls.Add(lvDownloads);
        Controls.Add(pnlSeparator);
        Controls.Add(pnlTop);
        Controls.Add(pnlBottom);
    }
}
