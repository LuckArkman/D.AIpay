namespace BoletoAdapter;

public class BoletoRegistrationResult
{
    public string Barcode { get; set; }
    public string DigitableLine { get; set; }
    public DateTime DueDate { get; set; }
}