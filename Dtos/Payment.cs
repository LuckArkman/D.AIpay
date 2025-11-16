namespace Dtos;

public class Payment {
    public Guid Id { get; init; }
    public decimal Amount { get; init; }
    public string Method { get; init; }
    public string Status { get; init; }
    public string? CardToken { get; init; }
    public int? Installments { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; set; }

    public Payment(decimal amount, string method, string status, string? cardToken = null, int? installments = null) {
        Id = Guid.NewGuid();
        Amount = amount;
        Method = method;
        Status = status;
        CardToken = cardToken;
        Installments = installments;
        CreatedAt = DateTime.UtcNow;
    }
    public Payment() {}
}