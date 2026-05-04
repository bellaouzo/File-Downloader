namespace FileDownloader.Models;

public enum DownloadStatus
{
    Pending,
    Downloading,
    Completed,
    Cancelled,
    Failed
}
