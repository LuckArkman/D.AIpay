namespace SharedKernel;

public record CardPaymentCreatedPayload(Guid PaymentId, string CardToken, decimal Amount, object Installments) : IPaymentPayload;