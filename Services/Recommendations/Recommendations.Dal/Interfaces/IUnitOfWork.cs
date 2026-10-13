using System.Data;

namespace Recommendations.Dal.Interfaces;

/// <summary>
/// Unit of Work: ОДНЕ з'єднання й ОДНА транзакція для всіх репозиторіїв однієї бізнес-операції.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    IUserRepository Users { get; }
    IWardrobeItemRepository WardrobeItems { get; }
    IOutfitRepository Outfits { get; }
    IRecommendationRepository Recommendations { get; }

    Task BeginTransactionAsync(IsolationLevel level = IsolationLevel.ReadCommitted, CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
