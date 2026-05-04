using FileDownloader.Interfaces;

namespace FileDownloader.Downloaders;

// Abstract base class for all downloader types.
// Holds the shared URL/path properties and helper methods so subclasses
// only need to implement DownloadAsync().
public abstract class DownloaderBase : IDownloadable
{
    protected readonly IProgressReporter ProgressReporter;

    public string Url { get; }
    public string DestinationPath { get; }

    protected DownloaderBase(string url, string destinationPath, IProgressReporter progressReporter)
    {
        Url = url;
        DestinationPath = destinationPath;
        ProgressReporter = progressReporter;
    }

    public abstract Task DownloadAsync(CancellationToken cancellationToken);

    protected static int CalculatePercentage(long bytesReceived, long totalBytes) =>
        totalBytes > 0 ? (int)(bytesReceived * 100 / totalBytes) : 0;

    protected static void EnsureDirectoryExists(string filePath)
    {
        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }
}
