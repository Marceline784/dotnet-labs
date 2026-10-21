namespace Wardrobe.Dal.Interfaces;

/// <summary>
/// Той самий контракт, що в Лаб. 02, але тепер його реалізує EF Core через DbSet&lt;T&gt;.
/// Update і Delete лише змінюють стан сутності в контексті (синхронні); у БД зміни потрапляють при SaveChangesAsync.
/// </summary>
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Delete(T entity);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
