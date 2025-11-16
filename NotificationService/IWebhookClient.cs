namespace NotificationService;

public interface IWebhookClient
{
    Task SendWebhookAsync(string url, object payload, string signature);
}