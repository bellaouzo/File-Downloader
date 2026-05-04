using FileDownloader.Interfaces;

namespace FileDownloader.Downloaders;

// Handles downloading files over HTTP/HTTPS.
// Streams in chunks so progress can be reported in real time.
public sealed class HttpFileDownloader : DownloaderBase
{
    // One shared client for the whole app — avoids socket exhaustion
    private static readonly HttpClient SharedClient = new() { Timeout = TimeSpan.FromMinutes(5) };
    private const int BufferSize = 81_920; // 80 KB chunks

    public HttpFileDownloader(string url, string destinationPath, IProgressReporter progressReporter)
        : base(url, destinationPath, progressReporter) { }

    public override async Task DownloadAsync(CancellationToken cancellationToken)
    {
        try
        {
            // ResponseHeadersRead lets us start reading before the whole response is buffered
            using HttpResponseMessage response = await SharedClient.GetAsync(
                Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            response.EnsureSuccessStatusCode();

            long totalBytes = response.Content.Headers.ContentLength ?? -1;
            long bytesReceived = 0;

            EnsureDirectoryExists(DestinationPath);

            await using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using FileStream fileStream = new(DestinationPath, FileMode.Create, FileAccess.Write,
                FileShare.None, BufferSize, useAsync: true);

            byte[] buffer = new byte[BufferSize];
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);

                bytesReceived += bytesRead;
                ProgressReporter.ReportProgress(bytesReceived, totalBytes,
                    CalculatePercentage(bytesReceived, totalBytes));
            }

            ProgressReporter.ReportCompletion(true, null);
        }
        catch (OperationCanceledException)
        {
            // Don't mark as failed — the queue manager will mark it Cancelled
            throw;
        }
        catch (Exception ex)
        {
            ProgressReporter.ReportCompletion(false, ex.Message);
            throw;
        }
    }
}
