using ReconciliationService;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Registra nosso downloader simulado
        services.AddSingleton<IDataDownloader, SimulatedDataDownloader>();
        
        // Registra o worker principal
        services.AddHostedService<ReconciliationWorker>();
    })
    .Build();

host.Run();