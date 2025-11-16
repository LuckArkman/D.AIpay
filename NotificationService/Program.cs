using NotificationService;
using Polly;
using Polly.Extensions.Http;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHttpClient<IWebhookClient, SimulatedWebhookClient>()
            .AddPolicyHandler((serviceProvider, request) => 
            {
                var logger = serviceProvider.GetRequiredService<ILogger<SimulatedWebhookClient>>();
                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Tenta 5 vezes com espera exponencial
                        onRetry: (outcome, timespan, retryAttempt, context) =>
                        {
                            logger.LogWarning(
                                "Falha ao enviar webhook. Status: {StatusCode}. Tentando novamente em {TimeSpan}s...",
                                outcome.Result?.StatusCode,
                                timespan.TotalSeconds);
                        });
            });
            
        services.AddHostedService<NotificationWorker>();
    })
    .Build();

host.Run();