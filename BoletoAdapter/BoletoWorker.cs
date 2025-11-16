using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BoletoAdapter;

public class BoletoWorker : BackgroundService
{
    private readonly ILogger<BoletoWorker> _logger;
    private readonly IBankClient _bankClient;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public BoletoWorker(ILogger<BoletoWorker> logger, IBankClient bankClient, IConfiguration config)
    {
        _logger = logger;
        _bankClient = bankClient;
        
        var factory = new ConnectionFactory() { HostName = config["MessageBroker:Host"], DispatchConsumersAsync = true };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel.ExchangeDeclare(exchange: "payment_events", type: ExchangeType.Topic);
        
        var queueName = "boleto_processing_queue";
        _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue: queueName, exchange: "payment_events", routingKey: "payment.boleto");
        
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            var paymentEvent = JsonSerializer.Deserialize<PaymentEvent>(message);
            
            _logger.LogInformation("[BoletoAdapter] Evento de boleto recebido para PaymentId: {PaymentId}", paymentEvent.Id);

            try
            {
                // Chama o cliente bancário para registrar o boleto
                var result = await _bankClient.RegisterBoletoAsync(paymentEvent);

                // Publica um novo evento com os dados do boleto registrado
                PublishResultEvent("boleto.registered", new { 
                    PaymentId = paymentEvent.Id,
                    result.Barcode,
                    result.DigitableLine,
                    result.DueDate
                });

                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BoletoAdapter] Falha crítica ao registrar boleto para o PaymentId {PaymentId}", paymentEvent.Id);
                
                // Publica um evento de falha para que o sistema saiba que este pagamento não pode prosseguir
                PublishResultEvent("payment.failed", new { PaymentId = paymentEvent.Id, Reason = "Failed to register boleto." });

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
        
        Guid paymentId = ((dynamic)payload).PaymentId;
        _logger.LogInformation("[BoletoAdapter] Evento de resultado '{RoutingKey}' publicado para PaymentId: {PaymentId}", routingKey, paymentId);
    }
    
    public override void Dispose()
    {
        _channel.Close();
        _connection.Close();
        base.Dispose();
    }
}

// DTO para o evento recebido
public record PaymentEvent(Guid Id, decimal Amount);