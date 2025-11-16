namespace ReconciliationService;

public interface IDataDownloader
{
    Task<Stream> DownloadCnabFileAsync();
    Task<Stream> DownloadAcquirerReportAsync();
}