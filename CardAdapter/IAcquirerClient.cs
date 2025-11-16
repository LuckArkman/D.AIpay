using SharedKernel;

namespace CardAdapter;

public interface IAcquirerClient
{
    Task<AuthorizationResult> AuthorizeAsync(CardPaymentCreatedPayload paymentEvent);
}