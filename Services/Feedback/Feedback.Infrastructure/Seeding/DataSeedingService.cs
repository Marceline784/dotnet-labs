using Feedback.Infrastructure.Persistence;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Feedback.Infrastructure.Seeding;

/// <summary>Запускає всі сідери під час старту (після створення індексів, бо hosted-сервіси стартують за порядком реєстрації).</summary>
public class DataSeedingService(
    IEnumerable<IDataSeeder> seeders,
    IOptions<MongoDbOptions> options,
    ILogger<DataSeedingService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        if (!options.Value.InitializeOnStartup) return;

        foreach (var seeder in seeders)
            await seeder.SeedAsync(ct);

        logger.LogInformation("MongoDB seeding finished");
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
