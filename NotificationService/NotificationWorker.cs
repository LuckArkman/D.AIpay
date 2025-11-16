using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService;

public class NotificationWorker : BackgroundService
{
    private readonly ILogger<NotificationWorker> _logger;
    private readonly IWebhookClient _webhookClient;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public NotificationWorker(ILogger<NotificationWorker> logger, IWebhookClient webhookClient, IConfiguration config)
    {
        _logger = logger;
        _webhookClient = webhookClient;
        
        var factory = new ConnectionFactory() { HostName = config["MessageBroker:Host"], DispatchConsumersAsync = true };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel.ExchangeDeclare(exchange: "payment_events", type: ExchangeType.Topic);
        
        var queueName = "notification_queue";
        _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
        
        // Se inscreve em múltiplos eventos que geram notificações
        _channel.QueueBind(queue: queueName, exchange: "payment_events", routingKey: "payment.paid");
        _channel.QueueBind(queue: queueName, exchange: "payment_events", routingKey: "payment.failed");
        _channel.QueueBind(queue: queueName, exchange: "payment_events", routingKey: "boleto.registered");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var eventType = ea.RoutingKey;
            var body = ea.Body.ToArray();
            var payloadString = Encoding.UTF8.GetString(body);
            
            _logger.LogInformation("[NotificationWorker] Evento '{EventType}' recebido para notificação.", eventType);

            try
            {
                // 1. Obter a URL e a chave secreta do lojista (simulado)
                // Em um sistema real, você buscaria isso no banco de dados com base no PaymentId.
                var merchantWebhookUrl = "https://lojista.com/webhook";
                var merchantSecretKey = "my-super-secret-key-for-hmac"; // Esta chave NUNCA deve estar no código. Use o Key Vault.

                // 2. Calcular a assinatura HMAC-SHA256
                var signature = CalculateSignature(payloadString, merchantSecretKey);

                // 3. Montar o payload final da notificação
                var notificationPayload = new 
                {
                    EventType = eventType,
                    Timestamp = DateTime.UtcNow,
                    Data = JsonDocument.Parse(payloadString) // Envia o payload original como um objeto aninhado
                };

                // 4. Enviar o webhook (com a política de Retry já configurada)
                await _webhookClient.SendWebhookAsync(merchantWebhookUrl, notificationPayload, signature);

                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[NotificationWorker] Falha crítica ao enviar notificação para o evento: {Payload}", payloadString);
                _channel.BasicNack(ea.DeliveryTag, false, false);
            }
        };
        
        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    private string CalculateSignature(string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        
        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        
        // Converte o hash para uma string hexadecimal
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
    
    public override void Dispose()
    {
        _channel.Close();
        _connection.Close();
        base.Dispose();
    }
}