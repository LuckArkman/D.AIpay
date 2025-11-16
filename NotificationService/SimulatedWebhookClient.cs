namespace NotificationService;

public class SimulatedWebhookClient : IWebhookClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SimulatedWebhookClient> _logger;

    public SimulatedWebhookClient(HttpClient httpClient, ILogger<SimulatedWebhookClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendWebhookAsync(string url, object payload, string signature)
    {
        _logger.LogInformation("[Webhook Client] Preparando para enviar webhook para a URL: {Url}", url);
        _logger.LogInformation("[Webhook Client] Assinatura calculada: {Signature}", signature);

        // Adiciona a assinatura ao header da requisição
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-Webhook-Signature", signature);

        // Simula uma falha para testar o Polly
        if (new Random().Next(1, 4) == 1) // 33% de chance de falha
        {
            _logger.LogWarning("[Webhook Client] Endpoint do lojista simulado está indisponível.");
            throw new HttpRequestException("Simulated merchant endpoint failure (503 Service Unavailable).");
        }

        // Em um sistema real, a linha abaixo seria usada:
        // var response = await _httpClient.PostAsJsonAsync(url, payload);
        // response.EnsureSuccessStatusCode();

        await Task.Delay(200); // Simula a requisição HTTP
        _logger.LogInformation("[Webhook Client] Webhook enviado com sucesso para a URL: {Url}", url);
    }
}