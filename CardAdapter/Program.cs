using CardAdapter;
using Polly;
using Polly.Extensions.Http;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // A política de retry é essencial para a comunicação com adquirentes.
        services.AddHttpClient<IAcquirerClient, SimulatedAcquirerClient>()
            .AddPolicyHandler((serviceProvider, request) => 
            {
                var logger = serviceProvider.GetRequiredService<ILogger<SimulatedAcquirerClient>>();
                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), 
                        onRetry: (outcome, timespan, retryAttempt, context) =>
                        {
                            logger.LogWarning(
                                "Falha na chamada ao adquirente. Status: {StatusCode}. Tentando novamente em {TimeSpan}s...",
                                outcome.Result?.StatusCode,
                                timespan.TotalSeconds);
                        });
            });
            
        services.AddHostedService<CardWorker>();
    })
    .Build();

host.Run();