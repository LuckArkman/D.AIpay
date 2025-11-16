namespace Records;

public record CardPaymentRequest(decimal Amount, string CardToken, InstallmentDetails Installments);