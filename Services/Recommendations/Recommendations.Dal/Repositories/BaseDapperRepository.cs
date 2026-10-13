using Dapper;
using Microsoft.Data.SqlClient;
using Recommendations.Dal.Interfaces;

namespace Recommendations.Dal.Repositories;

/// <summary>
/// Базовий клас для репозиторіїв на ADO.NET + Dapper: спільний CRUD пишеться один раз.
/// Ім'я таблиці передається з коду репозиторію (константа, НЕ вхідні дані користувача),
/// тому його підстановка в текст запиту безпечна. Усі ЗНАЧЕННЯ йдуть лише параметрами.
/// Видалення м'яке (IsDeleted = 1), як і в схемі з Лаб. 01.
/// </summary>
public abstract class BaseDapperRepository<T> : RepositoryBase, IGenericRepository<T> where T : class
{
    protected string Table { get; }

    protected BaseDapperRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction, string table)
        : base(connection, currentTransaction)
    {
        Table = table;
    }

    public virtual Task<T?> GetByIdAsync(long id, CancellationToken ct = default) =>
        GuardAsync<T?>(async () =>
        {
            var sql = $"SELECT * FROM {Table} WHERE Id = @Id AND IsDeleted = 0";
            await EnsureOpenAsync(ct);
            return await Connection.QuerySingleOrDefaultAsync<T>(
                new CommandDefinition(sql, new { Id = id }, Transaction, cancellationToken: ct));
        });

    public virtual Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default) =>
        GuardAsync<IReadOnlyList<T>>(async () =>
        {
            var sql = $"SELECT * FROM {Table} WHERE IsDeleted = 0 ORDER BY Id";
            await EnsureOpenAsync(ct);
            var rows = await Connection.QueryAsync<T>(
                new CommandDefinition(sql, transaction: Transaction, cancellationToken: ct));
            return rows.ToList();
        });

    /// <summary>Вставка залежить від сутності (процедура, identity тощо), тому її реалізує конкретний репозиторій.</summary>
    public abstract Task<long> AddAsync(T entity, CancellationToken ct = default);

    public virtual Task<bool> DeleteAsync(long id, CancellationToken ct = default) =>
        GuardAsync<bool>(async () =>
        {
            var sql = $"UPDATE {Table} SET IsDeleted = 1 WHERE Id = @Id AND IsDeleted = 0";
            await EnsureOpenAsync(ct);
            var rows = await Connection.ExecuteAsync(
                new CommandDefinition(sql, new { Id = id }, Transaction, cancellationToken: ct));
            return rows > 0;
        });
}
