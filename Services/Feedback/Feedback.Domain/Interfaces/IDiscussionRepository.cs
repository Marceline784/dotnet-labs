using Feedback.Domain.Entities;
using MongoDB.Bson;

namespace Feedback.Domain.Interfaces;

public interface IDiscussionRepository
{
    Task<Discussion?> GetByIdAsync(ObjectId id, CancellationToken ct = default);
    Task AddAsync(Discussion discussion, CancellationToken ct = default);
    Task UpdateAsync(Discussion discussion, CancellationToken ct = default);
}
