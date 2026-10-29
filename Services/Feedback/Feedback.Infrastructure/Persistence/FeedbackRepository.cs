using Feedback.Domain.Entities;
using Feedback.Domain.Exceptions;
using Feedback.Domain.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Feedback.Infrastructure.Persistence;

/// <summary>Реалізація доменного інтерфейсу на MongoDB Driver (шлях ЗАПИСУ та читання агрегату).</summary>
public class FeedbackRepository(MongoDbContext context) : IFeedbackRepository
{
    private readonly IMongoCollection<OutfitFeedback> _feedback = context.Feedback;

    public async Task<OutfitFeedback?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
        => await _feedback.Find(f => f.Id == id).FirstOrDefaultAsync(ct);

    public Task<bool> ExistsAsync(long outfitId, long authorUserId, CancellationToken ct = default)
        => _feedback.Find(f => f.OutfitId == outfitId && f.Author.UserId == authorUserId).AnyAsync(ct);

    public Task AddAsync(OutfitFeedback feedback, CancellationToken ct = default)
        => _feedback.InsertOneAsync(feedback, cancellationToken: ct);   // дубль за унікальним індексом -> MongoWriteException -> 409

    /// <summary>
    /// Оптимістична конкурентність: заміна застосовується лише до документа з очікуваною версією.
    /// Документи без поля version (старі, з Лаб. 01) вважаються версією 0.
    /// </summary>
    public async Task UpdateAsync(OutfitFeedback feedback, CancellationToken ct = default)
    {
        var expectedVersion = feedback.Version;
        feedback.BumpVersion();

        var filters = Builders<OutfitFeedback>.Filter;
        var versionFilter = expectedVersion == 0
            ? filters.Or(filters.Eq(f => f.Version, 0), filters.Exists(f => f.Version, false))
            : filters.Eq(f => f.Version, expectedVersion);

        var result = await _feedback.ReplaceOneAsync(
            filters.Eq(f => f.Id, feedback.Id) & versionFilter,
            feedback,
            cancellationToken: ct);

        if (result.MatchedCount == 0)                      // хтось змінив або видалив документ раніше
            throw new ConflictException($"Feedback {feedback.Id} was modified or deleted by someone else");
    }

    public async Task<bool> DeleteAsync(ObjectId id, CancellationToken ct = default)
    {
        var result = await _feedback.DeleteOneAsync(f => f.Id == id, ct);
        return result.DeletedCount > 0;
    }
}
