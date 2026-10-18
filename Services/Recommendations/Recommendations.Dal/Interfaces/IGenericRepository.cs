namespace Recommendations.Dal.Interfaces;

/// <summary>Спільні CRUD-операції для репозиторіїв на Dapper. Не містить SQL-типів: лише моделі.</summary>
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<long> AddAsync(T entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(long id, CancellationToken ct = default);
}
