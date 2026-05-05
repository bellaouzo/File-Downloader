using FileDownloader.Downloaders;
using FileDownloader.Models;

namespace FileDownloader.Services;

public class DownloadQueueManager
{
    private const int MaxConcurrentDownloads = 3;

    private readonly List<DownloadItem> _items = [];
    public IReadOnlyList<DownloadItem> Items => _items.AsReadOnly();

    // Gate lives here instead of inside StartPending so it stays alive between calls —
    // learned this the hard way: a new semaphore each time means adding URLs mid-download
    // ignores the 3-slot limit entirely
    private readonly SemaphoreSlim _gate = new(MaxConcurrentDownloads, MaxConcurrentDownloads);
    private CancellationTokenSource _cts = new();

    public event EventHandler? QueueStateChanged;

    public bool TryEnqueue(string url, string outputDirectory)
    {
        if (_items.Any(i => i.Url.Equals(url, StringComparison.OrdinalIgnoreCase)))
            return false;

        string fileName = DeriveFileName(url);
        _items.Add(new DownloadItem(url, Path.Combine(outputDirectory, fileName)));
        return true;
    }

    // Fire off all pending items — ones past the 3-slot limit just sit at WaitAsync
    // until something finishes and releases a slot
    public void StartPending()
    {
        foreach (DownloadItem item in _items.Where(i => i.Status == DownloadStatus.Pending).ToList())
            _ = RunItemAsync(item); // fire and forget — each item manages itself
    }

    private async Task RunItemAsync(DownloadItem item)
    {
        CancellationToken token = _cts.Token;

        try
        {
            await _gate.WaitAsync(token); // blocks until a download slot is free
        }
        catch (OperationCanceledException)
        {
            item.MarkCancelled();
            DeletePartialFile(item.DestinationPath);
            return;
        }

        try
        {
            item.MarkStarting(); // flip to Downloading now so all items appear to start together
            var downloader = new HttpFileDownloader(item.Url, item.DestinationPath, item);
            await downloader.DownloadAsync(token);
        }
        catch (OperationCanceledException)
        {
            item.MarkCancelled();
            DeletePartialFile(item.DestinationPath);
        }
        catch
        {
            // ReportCompletion(false, ...) already called inside HttpFileDownloader
        }
        finally
        {
            _gate.Release();
            QueueStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void CancelAll()
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource(); // fresh token so next download works
        QueueStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool TryRemove(DownloadItem item)
    {
        if (item.Status == DownloadStatus.Downloading) return false;
        return _items.Remove(item);
    }

    // Clears anything that's no longer active: completed, failed, and cancelled
    public void ClearDone()
    {
        _items.RemoveAll(i => i.Status is DownloadStatus.Completed or DownloadStatus.Failed or DownloadStatus.Cancelled);
        QueueStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void DeletePartialFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* file might still be held open for a moment, just leave it */ }
    }

    private static string DeriveFileName(string url)
    {
        try
        {
            string name = Path.GetFileName(new Uri(url).AbsolutePath);
            return string.IsNullOrWhiteSpace(name) ? $"download_{DateTime.Now:yyyyMMdd_HHmmss}" : name;
        }
        catch
        {
            return $"download_{DateTime.Now:yyyyMMdd_HHmmss}";
        }
    }
}
