namespace SharedKernel;

public record BoletoRegisteredPayload(Guid PaymentId, string Barcode, string DigitableLine, DateTime DueDate) : IPaymentPayload;