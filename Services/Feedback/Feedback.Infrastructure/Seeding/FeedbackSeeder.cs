using Feedback.Domain.Entities;
using Feedback.Domain.Enums;
using Feedback.Domain.ValueObjects;
using Feedback.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Feedback.Infrastructure.Seeding;

/// <summary>
/// Початкові дані (ті самі сценарії, що в Лаб. 01): повний відгук із вкладеними масивами й обговоренням,
/// короткий із температурною нотаткою та мінімальні. ІДЕМПОТЕНТНИЙ: якщо колекція вже наповнена, нічого не робить.
/// Використовує bulk-операцію InsertManyAsync.
/// </summary>
public class FeedbackSeeder(MongoDbContext context) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await context.Feedback.EstimatedDocumentCountAsync(cancellationToken: ct) > 0)
            return;                                              // уже наповнено

        var olena = new Author(1, "Олена");
        var max = new Author(2, "Макс");

        // (1) повний відгук: items, photos, comments, посилання на обговорення
        var full = new OutfitFeedback(
            outfitId: 1, author: olena, feeling: Feeling.Comfortable, rating: 5,
            text: "Вітровка і светр виручили: зранку прохолодно, а вдень светр лишився в рюкзаку.",
            items: new[]
            {
                new ItemSnapshot(2, "Легка вітровка", "Top"),
                new ItemSnapshot(3, "Вовняний светр", "Top"),
                new ItemSnapshot(7, "Класичні джинси", "Bottom"),
                new ItemSnapshot(10, "Білі кросівки", "Shoes")
            });
        full.AddPhoto("fb1_a.jpg");
        full.AddPhoto("fb1_b.jpg");
        full.AddComment("Андрій", "Класний образ");
        full.AddComment("Марія", "Де такий светр?");

        var discussion = new Discussion("Як носити шари восени", olena, full.Id, new[] { "шари", "осінь" });
        discussion.RegisterMessage();
        full.AttachDiscussion(discussion.Id);

        // (2) короткий відгук із температурною нотаткою (поле є не в усіх документах)
        var cold = new OutfitFeedback(
            outfitId: 4, author: max, feeling: Feeling.Cold,
            text: "Було холодніше, ніж показувала погода.",
            temperatureNote: new TemperatureNote(-2, 3));

        // (3) і (4) мінімальні відгуки
        var hot = new OutfitFeedback(outfitId: 5, author: olena, feeling: Feeling.Hot, rating: 3);
        var minimal = new OutfitFeedback(outfitId: 6, author: max, feeling: Feeling.Comfortable, rating: 4);

        await context.Feedback.InsertManyAsync(new[] { full, cold, hot, minimal }, cancellationToken: ct);
        await context.Discussions.InsertOneAsync(discussion, cancellationToken: ct);
    }
}
