using FileDownloader.Models;
using FileDownloader.Services;

namespace FileDownloader.Forms;

public partial class MainForm : Form
{
    private readonly DownloadQueueManager _queue = new();
    private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 250 };

    public MainForm()
    {
        InitializeComponent();

        _refreshTimer.Tick += (_, _) => RefreshListView();
        _refreshTimer.Start();

        _queue.QueueStateChanged += (_, _) => SafeRefresh();

        btnDownload.Click       += BtnDownload_Click;
        btnBrowse.Click         += BtnBrowse_Click;
        btnCancelAll.Click      += BtnCancelAll_Click;
        btnRemoveSelected.Click += BtnRemoveSelected_Click;
        btnClearDone.Click      += BtnClearDone_Click;

        // Ctrl+Enter submits — plain Enter adds a newline so you can paste multiple URLs
        txtUrl.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter && e.Control)
            {
                BtnDownload_Click(null, EventArgs.Empty);
                e.SuppressKeyPress = true;
            }
        };

        // Double-buffer the ListView so the 250ms rebuild doesn't flash
        typeof(ListView)
            .GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(lvDownloads, true);
    }

    private void BtnDownload_Click(object? sender, EventArgs e)
    {
        string raw = txtUrl.Text.Trim();
        if (string.IsNullOrWhiteSpace(raw)) return;

        if (string.IsNullOrWhiteSpace(txtOutputDir.Text))
        {
            MessageBox.Show("Choose a save folder first.", "No Folder Selected",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!Directory.Exists(txtOutputDir.Text))
        {
            var answer = MessageBox.Show(
                $"This folder doesn't exist yet:\n{txtOutputDir.Text}\n\nCreate it?",
                "Folder Not Found", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer == DialogResult.Yes)
                Directory.CreateDirectory(txtOutputDir.Text);
            else
                return;
        }

        string[] urls = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int added = 0;

        foreach (string url in urls)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                MessageBox.Show($"Not a valid URL:\n{url}", "Invalid URL",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                continue;
            }

            if (_queue.TryEnqueue(url, txtOutputDir.Text))
                added++;
        }

        if (added > 0)
        {
            txtUrl.Clear();
            _queue.StartPending();
            RefreshListView();
        }
    }

    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description            = "Select download destination folder",
            UseDescriptionForTitle = true,
            SelectedPath           = Directory.Exists(txtOutputDir.Text) ? txtOutputDir.Text : ""
        };
        if (dialog.ShowDialog() == DialogResult.OK)
            txtOutputDir.Text = dialog.SelectedPath;
    }

    private void BtnCancelAll_Click(object? sender, EventArgs e) => _queue.CancelAll();

    private void BtnRemoveSelected_Click(object? sender, EventArgs e)
    {
        foreach (ListViewItem lvi in lvDownloads.SelectedItems)
        {
            if (lvi.Tag is DownloadItem item)
                _queue.TryRemove(item);
        }
        RefreshListView();
    }

    private void BtnClearDone_Click(object? sender, EventArgs e)
    {
        _queue.ClearDone();
        RefreshListView();
    }

    private void SafeRefresh()
    {
        // BeginInvoke instead of Invoke here — Invoke was blocking the download thread
        // every 80 KB waiting for the UI to redraw, which Rider flagged as a perf issue.
        // BeginInvoke posts the update as a message and returns right away.
        if (InvokeRequired)
            BeginInvoke(RefreshListView);
        else
            RefreshListView();
    }

    private void RefreshListView()
    {
        // Save selected URLs before clearing so we can restore the highlight after rebuild —
        // Items.Clear() destroys selection state and the 250ms timer was wiping it constantly
        var selectedUrls = lvDownloads.SelectedItems
            .Cast<ListViewItem>()
            .Select(lvi => (lvi.Tag as DownloadItem)?.Url)
            .ToHashSet();

        lvDownloads.BeginUpdate();
        lvDownloads.Items.Clear();

        foreach (DownloadItem item in _queue.Items)
        {
            var lvi = new ListViewItem(item.FileName) { Tag = item };
            lvi.SubItems.Add(item.Status.ToString());
            lvi.SubItems.Add(item.Percentage > 0 || item.Status == DownloadStatus.Downloading
                ? $"{item.Percentage}%"
                : "-");
            lvi.SubItems.Add(item.FormattedSpeed);  // blank when not downloading
            lvi.SubItems.Add(item.FormattedSize);
            lvi.SubItems.Add(item.ErrorMessage ?? item.Url);

            lvi.ForeColor = item.Status switch
            {
                DownloadStatus.Completed   => Color.Green,
                DownloadStatus.Failed      => Color.Crimson,
                DownloadStatus.Cancelled   => Color.Crimson,   // red — cancelled is a stopped state
                DownloadStatus.Downloading => Color.DodgerBlue,
                _                          => lvDownloads.ForeColor
            };

            if (selectedUrls.Contains(item.Url))
                lvi.Selected = true;

            lvDownloads.Items.Add(lvi);
        }

        lvDownloads.EndUpdate();
        UpdateStatusBar();
    }

    private void UpdateStatusBar()
    {
        int total     = _queue.Items.Count;
        int completed = _queue.Items.Count(i => i.Status == DownloadStatus.Completed);
        int failed    = _queue.Items.Count(i => i.Status == DownloadStatus.Failed);
        int active    = _queue.Items.Count(i => i.Status == DownloadStatus.Downloading);

        btnCancelAll.Enabled   = active > 0;
        btnCancelAll.BackColor = active > 0 ? Color.FromArgb(196, 43, 28) : SystemColors.Control;
        btnCancelAll.ForeColor = active > 0 ? Color.White : SystemColors.ControlText;

        if (total == 0)
            lblStatus.Text = "Ready";
        else
            lblStatus.Text = $"{active} downloading  •  {completed} done  •  {failed} failed  •  {total} total";

        // Reset to zero if nothing is running or finished — covers the "cancelled all" case
        // where items still have partial BytesReceived values but nothing is actually progressing
        if (active == 0 && completed == 0)
        {
            progressOverall.Value = 0;
            return;
        }

        // Only count items that are actually making progress — cancelled/failed ones still
        // carry their partial byte counts which would freeze the bar at a wrong value
        var relevant = _queue.Items
            .Where(i => i.Status is DownloadStatus.Downloading
                                 or DownloadStatus.Completed
                                 or DownloadStatus.Pending)
            .ToList();

        long totalExpected = relevant.Sum(i => i.TotalBytes > 0 ? i.TotalBytes : 0);
        long totalReceived = relevant.Sum(i => i.BytesReceived);

        int pct = totalExpected > 0
            ? (int)(totalReceived * 100 / totalExpected)
            : completed * 100 / relevant.Count;

        progressOverall.Value = Math.Clamp(pct, 0, 100);
    }
}
