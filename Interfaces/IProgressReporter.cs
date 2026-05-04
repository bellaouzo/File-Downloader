namespace FileDownloader.Interfaces;

public interface IProgressReporter
{
    void ReportProgress(long bytesReceived, long totalBytes, int percentage);
    void ReportCompletion(bool success, string? errorMessage);
}
