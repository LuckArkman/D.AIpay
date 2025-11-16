using System.Data;
using System.Text;
using System.Text.Json;
using Dapper;
using Data;
using Dtos;
using Microsoft.OpenApi.Models;
using Npgsql;
using RabbitMQ.Client;
using Records;
using SharedKernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IDbConnection>(sp => 
    new NpgsqlConnection(builder.Configuration.GetConnectionString("Database")));
builder.Services.AddHttpClient();
builder.Services.AddHostedService<DatabaseInitializer>();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "🚀 D.AIpay",
        Version = "v1",
        Description = "API de processamento de pagamentos distribuídos via D.AIpay",
        Contact = new OpenApiContact
        {
            Name = "Equipe D.AIpay",
            Email = "contato@daipay.io"
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --- 2. ENDPOINTS DA API (VERSÃO 1) ---
// ... (o restante do seu código permanece exatamente o mesmo)
var apiV1 = app.MapGroup("/api/v1/payments");

// Endpoint para criar um pagamento com Cartão
apiV1.MapPost("/card", async (CardPaymentRequest req, IDbConnection db, IHttpClientFactory cf, IConfiguration config) =>
{
    if (req.Amount <= 0) return Results.BadRequest("Invalid amount.");

    var riskClient = cf.CreateClient();
    var riskResponse = await riskClient.PostAsJsonAsync("http://riskanalysis-service/analyze", new { req.Amount });
    if (!riskResponse.IsSuccessStatusCode) return Results.Problem("Risk analysis service unavailable.", statusCode: 503);
    
    var riskDecision = await riskResponse.Content.ReadFromJsonAsync<RiskDecision>();
    if (riskDecision?.Status != "APPROVED")
    {
        return Results.UnprocessableEntity(new { Error = $"Transaction declined by risk analysis: {riskDecision?.Reason}" });
    }

    var payment = new Payment(req.Amount, "Card", "Pending", req.CardToken, req.Installments.Count);
    var sql = "INSERT INTO Payments (Id, Amount, Method, Status, CardToken, Installments, CreatedAt) VALUES (@Id, @Amount, @Method, @Status, @CardToken, @Installments, @CreatedAt)";
    await db.ExecuteAsync(sql, payment);

    var payload = new CardPaymentCreatedPayload(payment.Id, req.CardToken, req.Amount, req.Installments);
    PublishEvent(config, "payment.card", payload);

    return Results.Accepted($"/api/v1/payments/{payment.Id}", new { PaymentId = payment.Id, Status = payment.Status });
});

app.Run();

void PublishEvent(IConfiguration config, string routingKey, object payload)
{
    var factory = new ConnectionFactory() { HostName = config["MessageBroker:Host"] };
    using var connection = factory.CreateConnection();
    using var channel = connection.CreateModel();
    
    channel.ExchangeDeclare(exchange: "payment_events", type: ExchangeType.Topic);
    var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
    channel.BasicPublish(exchange: "payment_events", routingKey: routingKey, body: body);
}