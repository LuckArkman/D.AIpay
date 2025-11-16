using System.Text;

namespace ReconciliationService;

public class SimulatedDataDownloader : IDataDownloader
{
    private readonly ILogger<SimulatedDataDownloader> _logger;

    public SimulatedDataDownloader(ILogger<SimulatedDataDownloader> logger)
    {
        _logger = logger;
    }

    public Task<Stream> DownloadCnabFileAsync()
    {
        _logger.LogInformation("[Data Downloader] Simulando download de arquivo de retorno CNAB...");
        
        // Simula um arquivo CNAB simples com IDs de pagamento.
        // Em um sistema real, isso seria um layout complexo (FEBRABAN 240/400).
        var cnabContent = $"{Guid.NewGuid()}\n{Guid.NewGuid()}\n{Guid.NewGuid()}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(cnabContent));
        
        return Task.FromResult<Stream>(stream);
    }

    public Task<Stream> DownloadAcquirerReportAsync()
    {
        _logger.LogInformation("[Data Downloader] Simulando download de relatório de adquirente (CSV)...");
        
        // Simula um relatório CSV com dados financeiros.
        var csvContent = new StringBuilder();
        csvContent.AppendLine("PaymentId,GrossAmount,Fee,NetAmount,Status");
        csvContent.AppendLine($"{Guid.NewGuid()},150.00,4.50,145.50,Settled");
        csvContent.AppendLine($"{Guid.NewGuid()},75.80,2.27,73.53,Settled");
        csvContent.AppendLine($"{Guid.NewGuid()},500.00,12.50,487.50,Chargeback");
        
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));
        return Task.FromResult<Stream>(stream);
    }
}