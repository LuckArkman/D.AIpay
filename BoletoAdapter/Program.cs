using BoletoAdapter;
using Polly;
using Polly.Extensions.Http;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHttpClient<IBankClient, SimulatedBankClient>()
            .AddPolicyHandler((serviceProvider, request) => 
            {
                var logger = serviceProvider.GetRequiredService<ILogger<SimulatedBankClient>>();
                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), 
                        onRetry: (outcome, timespan, retryAttempt, context) =>
                        {
                            logger.LogWarning(
                                "Falha na comunicação com o sistema bancário. Status: {StatusCode}. Tentando novamente em {TimeSpan}s...",
                                outcome.Result?.StatusCode,
                                timespan.TotalSeconds);
                        });
            });
            
        services.AddHostedService<BoletoWorker>();
    })
    .Build();

host.Run();