using System.Globalization;
using System.Text;
using System.Text.Json;
using CsvHelper;
using RabbitMQ.Client;

namespace ReconciliationService;

public class ReconciliationWorker : BackgroundService
{
    private readonly ILogger<ReconciliationWorker> _logger;
    private readonly IDataDownloader _dataDownloader;
    private readonly IConfiguration _config;

    public ReconciliationWorker(ILogger<ReconciliationWorker> logger, IDataDownloader dataDownloader, IConfiguration config)
    {
        _logger = logger;
        _dataDownloader = dataDownloader;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reconciliation Service iniciado.");
        
        // Aguarda um minuto antes de iniciar o primeiro ciclo para dar tempo para outros serviços subirem.
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        
        // Usa um timer para executar a rotina periodicamente (ex: a cada 24 horas).
        // Para testes, vamos usar um intervalo menor (ex: a cada 2 minutos).
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(2));

        do
        {
            _logger.LogInformation("Iniciando ciclo de conciliação em {time}", DateTimeOffset.Now);
            
            await ReconcileBoletosAsync();
            await ReconcileCardsAsync();

            _logger.LogInformation("Ciclo de conciliação finalizado. Próxima execução em {interval}.", timer.Period);
        } 
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ReconcileBoletosAsync()
    {
        _logger.LogInformation("--- Iniciando conciliação de Boletos (CNAB) ---");
        try
        {
            await using var cnabStream = await _dataDownloader.DownloadCnabFileAsync();
            using var reader = new StreamReader(cnabStream);
            
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (Guid.TryParse(line.Trim(), out var paymentId))
                {
                    // Publica o evento 'payment.paid' para cada boleto confirmado.
                    PublishEvent("payment.paid", new { PaymentId = paymentId });
                    _logger.LogInformation("Boleto pago conciliado para o PaymentId: {PaymentId}", paymentId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar arquivo de conciliação de boletos.");
        }
    }

    private async Task ReconcileCardsAsync()
    {
        _logger.LogInformation("--- Iniciando conciliação de Cartões (Relatório Adquirente) ---");
        try
        {
            await using var reportStream = await _dataDownloader.DownloadAcquirerReportAsync();
            using var reader = new StreamReader(reportStream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            var records = csv.GetRecords<AcquirerTransaction>();
            foreach (var record in records)
            {
                // Para transações de cartão, não publicamos um evento genérico.
                // Em um sistema real, faríamos uma chamada a um endpoint específico do
                // CorePaymentService para atualizar os dados financeiros.
                // Ex: PUT /api/v1/payments/{id}/financials
                _logger.LogInformation(
                    "Transação de cartão conciliada: Id={PaymentId}, ValorLíquido={NetAmount}, Status={Status}",
                    record.PaymentId, record.NetAmount, record.Status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar relatório de conciliação de cartões.");
        }
    }

    private void PublishEvent(string routingKey, object payload)
    {
        var factory = new ConnectionFactory() { HostName = _config["MessageBroker:Host"] };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        
        channel.ExchangeDeclare(exchange: "payment_events", type: ExchangeType.Topic);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        channel.BasicPublish(exchange: "payment_events", routingKey: routingKey, body: body);
    }
}