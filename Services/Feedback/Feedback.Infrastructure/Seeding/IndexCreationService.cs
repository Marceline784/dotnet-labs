using Feedback.Domain.Entities;
using Feedback.Infrastructure.Persistence;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Feedback.Infrastructure.Seeding;

/// <summary>
/// Автоматично створює індекси під час старту застосунку. Повторне створення наявного індексу безпечне:
/// MongoDB його не дублює. Назви й параметри збігаються з індексами з Лаб. 01, тому конфліктів немає.
/// </summary>
public class IndexCreationService(
    MongoDbContext context,
    IOptions<MongoDbOptions> options,
    ILogger<IndexCreationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        if (!options.Value.InitializeOnStartup) return;

        var keys = Builders<OutfitFeedback>.IndexKeys;
        var partial = new BsonDocumentFilterDefinition<OutfitFeedback>(
            new BsonDocument("photos", new BsonDocument("$exists", true)));

        var models = new[]
        {
            // один відгук від автора на образ (цілісність даних)
            new CreateIndexModel<OutfitFeedback>(
                keys.Ascending("outfitId").Ascending("author.userId"),
                new CreateIndexOptions { Name = "ux_feedback_outfit_user", Unique = true }),

            // «відгуки користувача, найновіші першими»
            new CreateIndexModel<OutfitFeedback>(
                keys.Ascending("author.userId").Descending("createdAt"),
                new CreateIndexOptions { Name = "ix_feedback_user_date" }),

            // розподіл відчуттів для уточнення чутливості до холоду
            new CreateIndexModel<OutfitFeedback>(
                keys.Ascending("author.userId").Ascending("feeling").Descending("createdAt"),
                new CreateIndexOptions { Name = "ix_feedback_user_feeling" }),

            // складений індекс: «останні відгуки про образ»
            new CreateIndexModel<OutfitFeedback>(
                keys.Ascending("outfitId").Descending("createdAt"),
                new CreateIndexOptions { Name = "ix_feedback_outfit_date" }),

            // частковий: лише відгуки з фото (поле є не в усіх документах)
            new CreateIndexModel<OutfitFeedback>(
                keys.Descending("createdAt"),
                new CreateIndexOptions<OutfitFeedback> { Name = "ix_feedback_with_photos", PartialFilterExpression = partial }),

            // повнотекстовий пошук по тексту відгуку
            new CreateIndexModel<OutfitFeedback>(
                keys.Text("text"),
                new CreateIndexOptions { Name = "tx_feedback_text", DefaultLanguage = "none" })
        };

        await CreateAsync(() => context.Feedback.Indexes.CreateManyAsync(models, ct), "outfitFeedback");

        var discussionKeys = Builders<Discussion>.IndexKeys;
        await CreateAsync(() => context.Discussions.Indexes.CreateOneAsync(
            new CreateIndexModel<Discussion>(discussionKeys.Descending("lastMessageAt"),
                new CreateIndexOptions { Name = "ix_discussions_last" }), cancellationToken: ct), "discussions");

        logger.LogInformation("MongoDB indexes are ready");
    }

    private async Task CreateAsync(Func<Task> create, string collection)
    {
        try
        {
            await create();
        }
        catch (MongoCommandException ex) when (ex.Code is 85 or 86)
        {
            // той самий індекс уже існує з іншою назвою або параметрами: не критично, працюємо з наявним
            logger.LogWarning("An equivalent index already exists in {Collection}: {Message}", collection, ex.Message);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
