using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Recommendations.Dal.Interfaces;
using Recommendations.Domain.Models;

namespace Recommendations.Dal.Repositories;

public sealed class RecommendationRepository : BaseDapperRepository<Recommendation>, IRecommendationRepository
{
    // рекомендація + знімок погоди (1:1) одним запитом
    private const string WithWeatherSql = @"
        SELECT r.Id, r.UserId, r.ForDate, r.CreatedAt,
               w.RecommendationId, w.City, w.TempMin, w.TempMax, w.FeelsLikeMin, w.FeelsLikeMax,
               w.PrecipProbability, w.WindSpeedMs, w.UvIndex, w.Condition
        FROM dbo.Recommendations r
        LEFT JOIN dbo.WeatherSnapshots w ON w.RecommendationId = r.Id";

    public RecommendationRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction, "dbo.Recommendations") { }

    /// <summary>
    /// Створює рекомендацію ТА знімок погоди процедурою usp_CreateRecommendation (потрібен entity.Weather).
    /// Повертає Id нової рекомендації.
    /// </summary>
    public override Task<long> AddAsync(Recommendation entity, CancellationToken ct = default) =>
        GuardAsync<long>(async () =>
        {
            var w = entity.Weather
                ?? throw new ArgumentException("Recommendation.Weather is required", nameof(entity));

            await EnsureOpenAsync(ct);

            var p = new DynamicParameters();
            p.Add("UserId", entity.UserId, DbType.Int64);
            p.Add("ForDate", entity.ForDate.Date, DbType.Date);
            p.Add("City", w.City, DbType.String, size: 100);
            p.Add("TempMin", w.TempMin, DbType.Decimal, precision: 4, scale: 1);
            p.Add("TempMax", w.TempMax, DbType.Decimal, precision: 4, scale: 1);
            p.Add("FeelsLikeMin", w.FeelsLikeMin, DbType.Decimal, precision: 4, scale: 1);
            p.Add("FeelsLikeMax", w.FeelsLikeMax, DbType.Decimal, precision: 4, scale: 1);
            p.Add("PrecipProbability", w.PrecipProbability, DbType.Byte);
            p.Add("WindSpeedMs", w.WindSpeedMs, DbType.Decimal, precision: 4, scale: 1);
            p.Add("UvIndex", w.UvIndex, DbType.Decimal, precision: 3, scale: 1);
            p.Add("Condition", w.Condition, DbType.String, size: 20);
            p.Add("RecommendationId", dbType: DbType.Int64, direction: ParameterDirection.Output);

            await Connection.ExecuteAsync(new CommandDefinition(
                "dbo.usp_CreateRecommendation", p, Transaction,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

            return p.Get<long>("RecommendationId");
        });

    public Task<Recommendation?> GetWithWeatherAsync(long id, CancellationToken ct = default) =>
        GuardAsync<Recommendation?>(async () =>
        {
            var sql = WithWeatherSql + " WHERE r.Id = @Id AND r.IsDeleted = 0";
            await EnsureOpenAsync(ct);

            var rows = await Connection.QueryAsync<Recommendation, WeatherSnapshot?, Recommendation>(
                new CommandDefinition(sql, new { Id = id }, Transaction, cancellationToken: ct),
                (rec, weather) =>
                {
                    rec.Weather = weather;
                    return rec;
                },
                splitOn: "RecommendationId");

            return rows.FirstOrDefault();
        });

    public Task<IReadOnlyList<Recommendation>> GetByUserAsync(long userId, int skip, int take, CancellationToken ct = default) =>
        GuardAsync<IReadOnlyList<Recommendation>>(async () =>
        {
            var sql = WithWeatherSql + @"
                WHERE r.UserId = @UserId AND r.IsDeleted = 0
                ORDER BY r.ForDate DESC, r.Id DESC
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
            await EnsureOpenAsync(ct);

            var rows = await Connection.QueryAsync<Recommendation, WeatherSnapshot?, Recommendation>(
                new CommandDefinition(sql, new { UserId = userId, Skip = skip, Take = take }, Transaction, cancellationToken: ct),
                (rec, weather) =>
                {
                    rec.Weather = weather;
                    return rec;
                },
                splitOn: "RecommendationId");

            return rows.ToList();
        });
}
