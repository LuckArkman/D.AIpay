using PixAdapter;
using Polly;
using Polly.Extensions.Http;

// --- Simulação de um cliente para o PSP de PIX ---
IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // =======================================================================
        // CORREÇÃO: A configuração da política de Polly é movida para dentro do
        // AddHttpClient, onde temos acesso ao ServiceProvider e, consequentemente,
        // ao ILogger.
        // =======================================================================
        services.AddHttpClient<IPspPixClient, SimulatedPspPixClient>()
            .AddPolicyHandler((serviceProvider, request) => 
            {
                // Obtém o logger aqui
                var logger = serviceProvider.GetRequiredService<ILogger<SimulatedPspPixClient>>();

                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), 
                        onRetry: (outcome, timespan, retryAttempt, context) =>
                        {
                            // Usa o logger diretamente, sem precisar do GetLogger() no contexto.
                            logger.LogWarning(
                                "Falha na chamada ao PSP. Status: {StatusCode}. Tentando novamente em {TimeSpan}s. Tentativa {RetryAttempt}/{TotalRetries}",
                                outcome.Result?.StatusCode,
                                timespan.TotalSeconds,
                                retryAttempt,
                                3); // Total de retries
                        });
            });

        // Registra o nosso worker que consome a fila
        services.AddHostedService<PixWorker>();
    })
    .Build();

host.Run();

public interface IPspPixClient { /* ... (sem alterações) ... */ }
public class SimulatedPspPixClient : IPspPixClient { /* ... (sem alterações) ... */ }


// --- Configuração do Host do Worker Service ---