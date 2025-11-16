namespace BoletoAdapter;

public class SimulatedBankClient : IBankClient
{
    private readonly ILogger<SimulatedBankClient> _logger;

    public SimulatedBankClient(ILogger<SimulatedBankClient> logger)
    {
        _logger = logger;
    }

    public async Task<BoletoRegistrationResult> RegisterBoletoAsync(PaymentEvent paymentEvent)
    {
        _logger.LogInformation("[Bank Client] Comunicando com o sistema bancário para registrar boleto para o pagamento {PaymentId}...", paymentEvent.Id);

        // Simula uma falha transitória para testar a política de retry do Polly
        if (new Random().Next(1, 4) == 1) // 33% de chance de falha
        {
            _logger.LogWarning("[Bank Client] Sistema bancário simulado retornou um erro de comunicação.");
            throw new HttpRequestException("Simulated bank communication failure.");
        }

        await Task.Delay(800); // Simula latência de sistemas legados

        var dueDate = DateTime.UtcNow.AddDays(5);
        var result = new BoletoRegistrationResult
        {
            Barcode = "12345678901234567890123456789012345678901234",
            DigitableLine = "12345.67890 12345.678901 12345.678902 1 12345678901234",
            DueDate = dueDate
        };
        
        _logger.LogInformation("[Bank Client] Boleto para o pagamento {PaymentId} registrado com sucesso com vencimento em {DueDate}.", paymentEvent.Id, dueDate.ToShortDateString());
        return result;
    }
}