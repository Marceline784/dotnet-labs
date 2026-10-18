using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Recommendations.Dal.Interfaces;
using Recommendations.Domain.Models;

namespace Recommendations.Dal.Repositories;

public sealed class WardrobeItemRepository : BaseDapperRepository<WardrobeItem>, IWardrobeItemRepository
{
    public WardrobeItemRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction, "dbo.WardrobeItems") { }

    /// <summary>
    /// Додає або оновлює локальну копію речі (процедура usp_UpsertWardrobeItemCopy).
    /// Власник даних — сервіс «Гардероб»; тут імітується застосування події від нього.
    /// Застаріла подія (старіший SourceUpdatedAt) ігнорується процедурою.
    /// </summary>
    public override Task<long> AddAsync(WardrobeItem item, CancellationToken ct = default) =>
        GuardAsync<long>(async () =>
        {
            await EnsureOpenAsync(ct);
            await Connection.ExecuteAsync(new CommandDefinition(
                "dbo.usp_UpsertWardrobeItemCopy",
                new
                {
                    item.Id,
                    item.OwnerUserId,
                    item.Name,
                    item.SlotCode,
                    item.Layer,
                    item.Warmth,
                    item.MinTemp,
                    item.MaxTemp,
                    item.WaterResistance,
                    item.IsAvailable,
                    item.PrimaryImageUrl,
                    IsDeleted = false,
                    item.SourceUpdatedAt
                },
                Transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
            return item.Id;
        });

    public Task<IReadOnlyList<WardrobeItem>> GetByOwnerAsync(long userId, string? slotCode = null, CancellationToken ct = default) =>
        GuardAsync<IReadOnlyList<WardrobeItem>>(async () =>
        {
            const string sql = @"
                SELECT Id, OwnerUserId, Name, SlotCode, Layer, Warmth, MinTemp, MaxTemp,
                       WaterResistance, IsAvailable, PrimaryImageUrl, SourceUpdatedAt
                FROM dbo.WardrobeItems
                WHERE OwnerUserId = @UserId AND IsDeleted = 0
                  AND (@SlotCode IS NULL OR SlotCode = @SlotCode)
                ORDER BY SlotCode, Layer, Name";
            await EnsureOpenAsync(ct);
            var rows = await Connection.QueryAsync<WardrobeItem>(new CommandDefinition(
                sql, new { UserId = userId, SlotCode = slotCode }, Transaction, cancellationToken: ct));
            return rows.ToList();
        });

    public Task<IReadOnlyList<WardrobeItem>> GetEligibleAsync(long recommendationId, CancellationToken ct = default) =>
        GuardAsync<IReadOnlyList<WardrobeItem>>(async () =>
        {
            await EnsureOpenAsync(ct);
            var rows = await Connection.QueryAsync<WardrobeItem>(new CommandDefinition(
                "dbo.usp_GetEligibleItems",
                new { RecommendationId = recommendationId },
                Transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
            return rows.ToList();
        });
}
