namespace PixAdapter;

public class SimulatedPspPixClient : IPspPixClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SimulatedPspPixClient> _logger;

    public SimulatedPspPixClient(HttpClient httpClient, ILogger<SimulatedPspPixClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> GenerateChargeAsync(Guid paymentId, decimal amount)
    {
        _logger.LogInformation("[PSP Client] Comunicando com o PSP para gerar cobrança para o pagamento {PaymentId}...", paymentId);
        
        if (new Random().Next(1, 4) == 1)
        {
            _logger.LogWarning("[PSP Client] PSP simulado retornou um erro transitório.");
            throw new HttpRequestException("Simulated PSP API failure.");
        }

        await Task.Delay(500);
        var qrCodeData = $"pix.psp.com/qr/{Guid.NewGuid()}";
        _logger.LogInformation("[PSP Client] Cobrança PIX gerada com sucesso. QR Code: {QrCode}", qrCodeData);
        return qrCodeData;
    }
}