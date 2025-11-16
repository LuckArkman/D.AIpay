using System.Data;
using System.Reflection;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Data;

public class DatabaseInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly IHostEnvironment _environment;

    public DatabaseInitializer(IServiceProvider serviceProvider, ILogger<DatabaseInitializer> logger, IHostEnvironment environment)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _environment = environment;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando a inicialização do banco de dados a partir do arquivo...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var connection = scope.ServiceProvider.GetRequiredService<IDbConnection>();

            // =======================================================================
            // CORREÇÃO: Lendo o script diretamente do caminho do arquivo.
            // O caminho base é o diretório de conteúdo da aplicação (onde o executável está).
            var scriptPath = Path.Combine(_environment.ContentRootPath, "init.sql");
            
            if (!File.Exists(scriptPath))
            {
                _logger.LogError("Script de inicialização não encontrado no caminho: {ScriptPath}", scriptPath);
                return;
            }
            
            _logger.LogInformation("Lendo script de inicialização de: {ScriptPath}", scriptPath);
            var script = await File.ReadAllTextAsync(scriptPath, cancellationToken);
            // =======================================================================
            
            if (string.IsNullOrWhiteSpace(script))
            {
                _logger.LogWarning("O script de inicialização está vazio.");
                return;
            }

            await connection.ExecuteAsync(script);

            _logger.LogInformation("Banco de dados inicializado com sucesso.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao inicializar o banco de dados.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}