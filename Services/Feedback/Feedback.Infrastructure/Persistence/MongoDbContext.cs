using Feedback.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Feedback.Infrastructure.Persistence;

/// <summary>
/// Єдина точка доступу до MongoDB. Отримує налаштування через IOptions і надає колекції доменних сутностей.
/// Реєструється як Singleton: MongoClient потокобезпечний і керує пулом з'єднань.
/// </summary>
public class MongoDbContext
{
    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        MongoMappings.Register();      // BSON-мапінг реєструється один раз, до першого звернення до БД

        var client = new MongoClient(options.Value.ConnectionString);
        Database = client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoDatabase Database { get; }

    public IMongoCollection<OutfitFeedback> Feedback => Database.GetCollection<OutfitFeedback>("outfitFeedback");

    public IMongoCollection<Discussion> Discussions => Database.GetCollection<Discussion>("discussions");
}
