namespace SharedKernel;

public record PaymentCreatedPayload(Guid PaymentId, decimal Amount) : IPaymentPayload;