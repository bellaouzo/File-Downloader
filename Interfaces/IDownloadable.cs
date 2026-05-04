namespace FileDownloader.Interfaces;

public interface IDownloadable
{
    string Url { get; }
    string DestinationPath { get; }
    Task DownloadAsync(CancellationToken cancellationToken);
}
