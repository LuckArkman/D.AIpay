namespace SharedKernel;

public record Payment3dsRequiredPayload(Guid PaymentId, string? RedirectUrl) : IPaymentPayload;