using Recommendations.Domain.Models;

namespace Recommendations.Dal.Interfaces;

public interface IWardrobeItemRepository : IGenericRepository<WardrobeItem>
{
    /// <summary>Речі користувача (за потреби лише певного слота).</summary>
    Task<IReadOnlyList<WardrobeItem>> GetByOwnerAsync(long userId, string? slotCode = null, CancellationToken ct = default);

    /// <summary>Речі, придатні для погоди цієї рекомендації (процедура usp_GetEligibleItems).</summary>
    Task<IReadOnlyList<WardrobeItem>> GetEligibleAsync(long recommendationId, CancellationToken ct = default);
}
