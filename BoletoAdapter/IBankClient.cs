namespace BoletoAdapter;

public interface IBankClient
{
    Task<BoletoRegistrationResult> RegisterBoletoAsync(PaymentEvent paymentEvent);
}