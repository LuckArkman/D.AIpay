using SharedKernel;

namespace CardAdapter;

public class SimulatedAcquirerClient : IAcquirerClient
{
    private readonly ILogger<SimulatedAcquirerClient> _logger;

    public SimulatedAcquirerClient(ILogger<SimulatedAcquirerClient> logger)
    {
        _logger = logger;
    }

    public async Task<AuthorizationResult> AuthorizeAsync(CardPaymentCreatedPayload paymentEvent)
    {
        _logger.LogInformation("[Acquirer Client] Comunicando com o adquirente para autorizar pagamento {PaymentId} com token {CardToken}", 
            paymentEvent.PaymentId, paymentEvent.CardToken);

        await Task.Delay(1000); // Simula latência

        // Simulação de diferentes respostas do adquirente
        int random = new Random().Next(1, 11);
        if (random <= 6) // 60% de chance de aprovar
        {
            _logger.LogInformation("[Acquirer Client] Pagamento {PaymentId} autorizado com sucesso.", paymentEvent.PaymentId);
            return new AuthorizationResult { Status = "AUTHORIZED" };
        }
        if (random <= 8) // 20% de chance de recusar
        {
            _logger.LogWarning("[Acquirer Client] Pagamento {PaymentId} recusado por 'Saldo insuficiente'.", paymentEvent.PaymentId);
            return new AuthorizationResult { Status = "DECLINED", DeclineReason = "Insufficient funds" };
        }
        
        // 20% de chance de requerer 3DS
        _logger.LogInformation("[Acquirer Client] Pagamento {PaymentId} requer autenticação 3DS.", paymentEvent.PaymentId);
        return new AuthorizationResult { Status = "REQUIRES_3DS", RedirectUrl = $"https://bank.com/3ds-auth/{Guid.NewGuid()}" };
    }
}