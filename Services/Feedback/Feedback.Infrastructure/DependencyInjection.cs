using Feedback.Application.Common.Interfaces;
using Feedback.Domain.Interfaces;
using Feedback.Infrastructure.Persistence;
using Feedback.Infrastructure.Seeding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Feedback.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        MongoMappings.Register();

        services.Configure<MongoDbOptions>(config.GetSection("MongoDb"));

        // .NET Aspire передає рядок підключення як ConnectionStrings:FeedbackDb: він має пріоритет над секцією MongoDb
        services.PostConfigure<MongoDbOptions>(options =>
        {
            var fromHost = config.GetConnectionString("FeedbackDb");
            if (!string.IsNullOrWhiteSpace(fromHost))
                options.ConnectionString = fromHost;
        });

        services.AddSingleton<MongoDbContext>();

        services.AddScoped<IFeedbackRepository, FeedbackRepository>();
        services.AddScoped<IDiscussionRepository, DiscussionRepository>();
        services.AddScoped<IFeedbackQueries, FeedbackQueries>();

        services.AddSingleton<IDataSeeder, FeedbackSeeder>();

        // порядок важливий: спочатку індекси, потім сідінг
        services.AddHostedService<IndexCreationService>();
        services.AddHostedService<DataSeedingService>();

        return services;
    }
}
