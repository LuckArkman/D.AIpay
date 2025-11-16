namespace SharedKernel;

public record PaymentFailedPayload(Guid PaymentId, string? Reason) : IPaymentPayload;