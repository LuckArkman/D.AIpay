using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PixAdapter;

public class PixWorker : BackgroundService
{
    private readonly ILogger<PixWorker> _logger;
    private readonly IPspPixClient _pspClient;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public PixWorker(ILogger<PixWorker> logger, IPspPixClient pspClient, IConfiguration config)
    {
        _logger = logger;
        _pspClient = pspClient;
        
        var factory = new ConnectionFactory() { HostName = config["MessageBroker:Host"], DispatchConsumersAsync = true };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel.ExchangeDeclare(exchange: "payment_events", type: ExchangeType.Topic);
        
        var queueName = "pix_processing_queue";
        _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue: queueName, exchange: "payment_events", routingKey: "payment.pix");
        
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var paymentEvent = JsonSerializer.Deserialize<PaymentEvent>(message);

            _logger.LogInformation("Evento de pagamento PIX recebido para o PaymentId: {PaymentId}", paymentEvent.PaymentId);

            try
            {
                // Chama o cliente PSP (com a política de Retry já configurada)
                var qrCode = await _pspClient.GenerateChargeAsync(paymentEvent.PaymentId, paymentEvent.Amount);

                // Lógica de sucesso:
                // Em um sistema real, aqui você publicaria um novo evento (ex: 'pix.charge.created')
                // ou atualizaria o status no CorePaymentService via API.
                _logger.LogInformation("Processamento do PaymentId {PaymentId} concluído com sucesso.", paymentEvent.PaymentId);
                
                // Confirma ao RabbitMQ que a mensagem foi processada com sucesso e pode ser removida da fila.
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar o PaymentId {PaymentId} após todas as tentativas.", paymentEvent.PaymentId);
                
                // Informa ao RabbitMQ que a mensagem falhou.
                // O terceiro argumento 'requeue' é false para evitar um loop infinito de falhas.
                // A mensagem será descartada ou enviada para uma Dead Letter Exchange (se configurada).
                _channel.BasicNack(ea.DeliveryTag, false, false);
            }
        };
        
        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }
    
    public override void Dispose()
    {
        _channel.Close();
        _connection.Close();
        base.Dispose();
    }
}

public record PaymentEvent(Guid PaymentId, decimal Amount);