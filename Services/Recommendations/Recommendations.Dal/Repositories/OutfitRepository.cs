using System.Data;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using Recommendations.Dal.Interfaces;
using Recommendations.Domain.Models;

namespace Recommendations.Dal.Repositories;

public sealed class OutfitRepository : BaseDapperRepository<Outfit>, IOutfitRepository
{
    // один запит: образ + позиції (LEFT JOIN); splitOn = перший стовпець OutfitItem
    private const string OutfitWithItemsSql = @"
        SELECT o.Id, o.RecommendationId, o.CarouselPosition, o.FitScore, o.ItemsHash, o.Status, o.CreatedAt, o.UpdatedAt,
               i.OutfitId, i.WardrobeItemId, i.SlotCode, i.Layer, i.ItemName, i.ImageUrl, i.IsRemovable
        FROM dbo.Outfits o
        LEFT JOIN dbo.OutfitItems i ON i.OutfitId = o.Id";

    public OutfitRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction, "dbo.Outfits") { }

    /// <summary>
    /// Створює образ процедурою usp_AddOutfit: вона перевіряє власника, доступність речей, слоти,
    /// повтор образу за останні дні та сама пише знімок позицій і початкову історію статусів.
    /// Список речей береться з outfit.Items (WardrobeItemId).
    /// </summary>
    public override Task<long> AddAsync(Outfit outfit, CancellationToken ct = default) =>
        GuardAsync<long>(async () =>
        {
            await EnsureOpenAsync(ct);

            var itemIds = outfit.Items.Select(i => i.WardrobeItemId).ToArray();

            var p = new DynamicParameters();
            p.Add("RecommendationId", outfit.RecommendationId, DbType.Int64);
            p.Add("FitScore", outfit.FitScore, DbType.Decimal, precision: 5, scale: 2);
            p.Add("ItemIdsJson", JsonSerializer.Serialize(itemIds), DbType.String, size: -1);
            p.Add("OutfitId", dbType: DbType.Int64, direction: ParameterDirection.Output);

            await Connection.ExecuteAsync(new CommandDefinition(
                "dbo.usp_AddOutfit", p, Transaction,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

            return p.Get<long>("OutfitId");
        });

    public Task<Outfit?> GetWithItemsAsync(long outfitId, CancellationToken ct = default) =>
        GuardAsync<Outfit?>(async () =>
        {
            var sql = OutfitWithItemsSql + " WHERE o.Id = @OutfitId AND o.IsDeleted = 0";
            await EnsureOpenAsync(ct);

            Outfit? result = null;
            await Connection.QueryAsync<Outfit, OutfitItem?, Outfit>(
                new CommandDefinition(sql, new { OutfitId = outfitId }, Transaction, cancellationToken: ct),
                (outfit, item) =>
                {
                    result ??= outfit;                 // кожен рядок JOIN: той самий образ + одна позиція
                    if (item is not null) result.Items.Add(item);
                    return result;
                },
                splitOn: "OutfitId");
            return result;
        });

    public Task<IReadOnlyList<Outfit>> GetByRecommendationAsync(long recommendationId, CancellationToken ct = default) =>
        GuardAsync<IReadOnlyList<Outfit>>(async () =>
        {
            var sql = OutfitWithItemsSql + " WHERE o.RecommendationId = @RecommendationId AND o.IsDeleted = 0";
            await EnsureOpenAsync(ct);

            var map = new Dictionary<long, Outfit>();
            await Connection.QueryAsync<Outfit, OutfitItem?, Outfit>(
                new CommandDefinition(sql, new { RecommendationId = recommendationId }, Transaction, cancellationToken: ct),
                (outfit, item) =>
                {
                    if (!map.TryGetValue(outfit.Id, out var existing))
                    {
                        existing = outfit;
                        map[outfit.Id] = existing;
                    }
                    if (item is not null) existing.Items.Add(item);
                    return existing;
                },
                splitOn: "OutfitId");

            return map.Values.OrderBy(o => o.CarouselPosition).ToList();
        });

    public Task ChangeStatusAsync(long outfitId, string newStatus, string? changedBy = null, string? note = null,
        CancellationToken ct = default) =>
        GuardVoidAsync(async () =>
        {
            await EnsureOpenAsync(ct);
            await Connection.ExecuteAsync(new CommandDefinition(
                "dbo.usp_ChangeOutfitStatus",
                new { OutfitId = outfitId, NewStatus = newStatus, ChangedBy = changedBy, Note = note },
                Transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
        });

    public Task<IReadOnlyCollection<string>> GetRecentlyWornHashesAsync(long userId, DateTime forDate, int days,
        CancellationToken ct = default) =>
        GuardAsync<IReadOnlyCollection<string>>(async () =>
        {
            const string sql = @"
                SELECT DISTINCT o.ItemsHash
                FROM dbo.Outfits o
                JOIN dbo.Recommendations r ON r.Id = o.RecommendationId
                WHERE r.UserId = @UserId AND o.IsDeleted = 0
                  AND o.Status IN (N'Worn', N'Reviewed')
                  AND r.ForDate >= @From AND r.ForDate < @To";
            await EnsureOpenAsync(ct);

            var to = forDate.Date;
            var rows = await Connection.QueryAsync<string>(new CommandDefinition(
                sql, new { UserId = userId, From = to.AddDays(-days), To = to }, Transaction, cancellationToken: ct));
            return rows.ToList();
        });
}
