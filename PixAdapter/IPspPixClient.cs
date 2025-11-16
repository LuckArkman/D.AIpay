namespace PixAdapter;

public interface IPspPixClient
{
    Task<string> GenerateChargeAsync(Guid paymentId, decimal amount);
}