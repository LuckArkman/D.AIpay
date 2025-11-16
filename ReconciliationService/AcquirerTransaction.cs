namespace ReconciliationService;

public class AcquirerTransaction
{
    public Guid PaymentId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal Fee { get; set; }
    public decimal NetAmount { get; set; }
    public string Status { get; set; }
}