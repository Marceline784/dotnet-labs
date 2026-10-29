using Feedback.Application.Common.Interfaces;
using Feedback.Application.Common.Models;
using Feedback.Application.Common.Validation;
using Feedback.Application.Feedbacks.Dtos;
using Feedback.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Feedback.Infrastructure.Persistence;

/// <summary>
/// Шлях ЧИТАННЯ. Список будується aggregation pipeline ($match, $sort, $skip, $limit, $project):
/// фільтрація, пагінація й підрахунок коментарів виконує MongoDB, а з бази вибираються лише потрібні поля.
/// </summary>
public class FeedbackQueries(MongoDbContext context) : IFeedbackQueries
{
    private readonly IMongoCollection<OutfitFeedback> _feedback = context.Feedback;

    // проєкція: без тексту коментарів і масивів, лише лічильник коментарів
    private static readonly BsonDocument ListProjection = new()
    {
        { "outfitId", 1 },
        { "author.userId", 1 },
        { "author.displayName", 1 },
        { "feeling", 1 },
        { "rating", 1 },
        { "text", 1 },
        { "version", 1 },
        { "createdAt", 1 },
        { "commentsCount", new BsonDocument("$size",
            new BsonDocument("$ifNull", new BsonArray { "$comments", new BsonArray() })) }
    };

    public async Task<FeedbackDetailsDto?> GetByIdAsync(string id, CancellationToken ct)
    {
        var objectId = ObjectId.Parse(id);
        var f = await _feedback.Find(x => x.Id == objectId).FirstOrDefaultAsync(ct);
        if (f is null) return null;

        return new FeedbackDetailsDto(
            f.Id.ToString(), f.OutfitId, f.Author.UserId, f.Author.DisplayName, f.Feeling.ToString(),
            f.Rating, f.Text,
            f.TemperatureNote is null ? null : new TemperatureNoteDto(f.TemperatureNote.Felt, f.TemperatureNote.Forecast),
            f.Photos.ToList(),
            f.Items.Select(i => new ItemSnapshotDto(i.ItemId, i.Name, i.Slot)).ToList(),
            f.Comments.Select(c => new CommentDto(c.Author, c.Text, c.CreatedAt)).ToList(),
            f.DiscussionId?.ToString(), f.Version, f.CreatedAt, f.UpdatedAt);
    }

    public async Task<PagedResult<FeedbackDto>> GetListAsync(
        long? outfitId, long? authorUserId, string? feeling, string? search,
        int page, int pageSize, CancellationToken ct)
    {
        var filters = Builders<OutfitFeedback>.Filter;
        var filter = filters.Empty;

        if (outfitId is not null)
            filter &= filters.Eq(x => x.OutfitId, outfitId.Value);
        if (authorUserId is not null)
            filter &= filters.Eq(x => x.Author.UserId, authorUserId.Value);
        if (feeling is not null)
            filter &= filters.Eq(x => x.Feeling, FeelingParser.Parse(feeling));
        if (!string.IsNullOrWhiteSpace(search))
            filter &= filters.Text(search.Trim());            // повнотекстовий пошук через text-індекс

        var total = await _feedback.CountDocumentsAsync(filter, cancellationToken: ct);

        var documents = await _feedback.Aggregate()
            .Match(filter)
            .Sort(Builders<OutfitFeedback>.Sort.Descending(x => x.CreatedAt).Descending(x => x.Id))
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .Project<BsonDocument>(new BsonDocumentProjectionDefinition<OutfitFeedback, BsonDocument>(ListProjection))
            .ToListAsync(ct);

        var items = documents.Select(ToDto).ToList();
        return new PagedResult<FeedbackDto>(items, (int)total, page, pageSize);
    }

    private static FeedbackDto ToDto(BsonDocument d)
    {
        var author = d["author"].AsBsonDocument;
        return new FeedbackDto(
            d["_id"].AsObjectId.ToString(),
            d["outfitId"].ToInt64(),
            author["userId"].ToInt64(),
            author["displayName"].AsString,
            d["feeling"].AsString,
            d.TryGetValue("rating", out var rating) && !rating.IsBsonNull ? rating.ToInt32() : null,
            d.TryGetValue("text", out var text) && !text.IsBsonNull ? text.AsString : null,
            d.TryGetValue("commentsCount", out var count) ? count.ToInt32() : 0,
            d.TryGetValue("version", out var version) ? version.ToInt32() : 0,
            d["createdAt"].ToUniversalTime());
    }
}
