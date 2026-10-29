using Feedback.Domain.Entities;
using Feedback.Domain.Exceptions;
using Feedback.Domain.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Feedback.Infrastructure.Persistence;

public class DiscussionRepository(MongoDbContext context) : IDiscussionRepository
{
    private readonly IMongoCollection<Discussion> _discussions = context.Discussions;

    public async Task<Discussion?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
        => await _discussions.Find(d => d.Id == id).FirstOrDefaultAsync(ct);

    public Task AddAsync(Discussion discussion, CancellationToken ct = default)
        => _discussions.InsertOneAsync(discussion, cancellationToken: ct);

    public async Task UpdateAsync(Discussion discussion, CancellationToken ct = default)
    {
        var expectedVersion = discussion.Version;
        discussion.BumpVersion();

        var filters = Builders<Discussion>.Filter;
        var versionFilter = expectedVersion == 0
            ? filters.Or(filters.Eq(d => d.Version, 0), filters.Exists(d => d.Version, false))
            : filters.Eq(d => d.Version, expectedVersion);

        var result = await _discussions.ReplaceOneAsync(
            filters.Eq(d => d.Id, discussion.Id) & versionFilter, discussion, cancellationToken: ct);

        if (result.MatchedCount == 0)
            throw new ConflictException($"Discussion {discussion.Id} was modified or deleted by someone else");
    }
}
