namespace SharedKernel;

public record PaymentPaidPayload(Guid PaymentId, DateTime PaidAt) : IPaymentPayload;