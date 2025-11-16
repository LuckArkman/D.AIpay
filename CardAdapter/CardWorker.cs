using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedKernel;

namespace CardAdapter;

public class CardWorker : BackgroundService
{
    private readonly ILogger<CardWorker> _logger;
    private readonly IAcquirerClient _acquirerClient;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public CardWorker(ILogger<CardWorker> logger, IAcquirerClient acquirerClient, IConfiguration config)
    {
        _logger = logger;
        _acquirerClient = acquirerClient;
        
        var factory = new ConnectionFactory() { HostName = config["MessageBroker:Host"], DispatchConsumersAsync = true };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel.ExchangeDeclare(exchange: "payment_events", type: ExchangeType.Topic);
        
        var queueName = "card_processing_queue";
        _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue: queueName, exchange: "payment_events", routingKey: "payment.card");
        
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            var paymentEvent = JsonSerializer.Deserialize<CardPaymentCreatedPayload>(message);
            
            _logger.LogInformation("[CardAdapter] Evento de cartão recebido para PaymentId: {PaymentId}", paymentEvent.PaymentId);

            try
            {
                // Chama o cliente do adquirente para autorizar a transação
                var result = await _acquirerClient.AuthorizeAsync(paymentEvent);

                // Publica um novo evento com base no resultado
                switch (result.Status)
                {
                    case "AUTHORIZED":
                        PublishResultEvent("payment.paid", new { paymentEvent.PaymentId });
                        break;
                    case "DECLINED":
                        PublishResultEvent("payment.failed", new { paymentEvent.PaymentId, Reason = result.DeclineReason });
                        break;
                    case "REQUIRES_3DS":
                        PublishResultEvent("payment.3ds_required", new { paymentEvent.PaymentId, RedirectUrl = result.RedirectUrl });
                        break;
                }

                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CardAdapter] Falha crítica ao processar PaymentId {PaymentId}", paymentEvent.PaymentId);
                PublishResultEvent("payment.failed", new { paymentEvent.PaymentId, Reason = "Internal adapter error." });
                _channel.BasicNack(ea.DeliveryTag, false, false);
            }
        };
        
        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    private void PublishResultEvent(string routingKey, object payload)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        _channel.BasicPublish(exchange: "payment_events", routingKey: routingKey, body: body);
        Guid paymentId = ((dynamic)payload).Id;

        // 2. Usamos a variável fortemente tipada na chamada do método de extensão.
        //    Isso evita o erro "dynamically dispatched".
        _logger.LogInformation(
            "[CardAdapter] Evento de resultado '{RoutingKey}' publicado para PaymentId: {PaymentId}", 
            routingKey, 
            paymentId);
    }
    
    public override void Dispose()
    {
        _channel.Close();
        _connection.Close();
        base.Dispose();
    }
}

// DTOs para o evento recebido
public record CardPaymentEvent(Guid Id, string CardToken, decimal Amount, InstallmentDetails Installments);
public record InstallmentDetails(int Count);