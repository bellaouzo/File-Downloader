using FileDownloader.Interfaces;

namespace FileDownloader.Models;

// Represents a single file in the download queue.
// Implements IProgressReporter so the downloader can call back into this object directly.
public class DownloadItem : IProgressReporter
{
    public string Url { get; }
    public string DestinationPath { get; }
    public string FileName => Path.GetFileName(DestinationPath);
    public DownloadStatus Status { get; private set; } = DownloadStatus.Pending;
    public long BytesReceived { get; private set; }
    public long TotalBytes { get; private set; } = -1;
    public int Percentage { get; private set; }
    public string? ErrorMessage { get; private set; }

    // Speed tracking
    private DateTime _lastSpeedCheck = DateTime.MinValue;
    private long _lastSpeedBytes = 0;
    public double SpeedBytesPerSecond { get; private set; }

    public event EventHandler? StateChanged;

    public DownloadItem(string url, string destinationPath)
    {
        Url = url;
        DestinationPath = destinationPath;
    }

    public void ReportProgress(long bytesReceived, long totalBytes, int percentage)
    {
        Status = DownloadStatus.Downloading;
        BytesReceived = bytesReceived;
        TotalBytes = totalBytes;
        Percentage = percentage;

        var now = DateTime.UtcNow;
        if (_lastSpeedCheck == DateTime.MinValue)
        {
            _lastSpeedCheck = now;
            _lastSpeedBytes = bytesReceived;
        }
        else
        {
            double elapsed = (now - _lastSpeedCheck).TotalSeconds;
            if (elapsed >= 0.5)
            {
                SpeedBytesPerSecond = (bytesReceived - _lastSpeedBytes) / elapsed;
                _lastSpeedCheck = now;
                _lastSpeedBytes = bytesReceived;
            }
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ReportCompletion(bool success, string? errorMessage)
    {
        Status = success ? DownloadStatus.Completed : DownloadStatus.Failed;
        ErrorMessage = errorMessage;
        SpeedBytesPerSecond = 0;
        if (success) Percentage = 100;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    internal void MarkStarting()
    {
        Status = DownloadStatus.Downloading;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    internal void MarkCancelled()
    {
        Status = DownloadStatus.Cancelled;
        SpeedBytesPerSecond = 0;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public string FormattedSpeed =>
        SpeedBytesPerSecond <= 0 ? "" :
        SpeedBytesPerSecond < 1024 ? $"{SpeedBytesPerSecond:F0} B/s" :
        SpeedBytesPerSecond < 1024 * 1024 ? $"{SpeedBytesPerSecond / 1024:F1} KB/s" :
        $"{SpeedBytesPerSecond / (1024.0 * 1024):F1} MB/s";

    public string FormattedSize =>
        TotalBytes <= 0 ? "Unknown" :
        TotalBytes < 1024 ? $"{TotalBytes} B" :
        TotalBytes < 1024 * 1024 ? $"{TotalBytes / 1024.0:F1} KB" :
        TotalBytes < 1024L * 1024 * 1024 ? $"{TotalBytes / (1024.0 * 1024):F1} MB" :
        $"{TotalBytes / (1024.0 * 1024 * 1024):F1} GB";
}
