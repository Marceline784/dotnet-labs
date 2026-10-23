using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Wardrobe.Dal.Interfaces;

namespace Wardrobe.Dal.Repositories;

/// <summary>Async Generic Repository на DbSet&lt;T&gt;. Асинхронні всі методи, що звертаються до БД.</summary>
public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly WardrobeDbContext Context;
    protected readonly DbSet<T> Set;

    public GenericRepository(WardrobeDbContext context)
    {
        Context = context;
        Set = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(long id, CancellationToken ct = default)
        => await Set.FindAsync(new object[] { id }, ct);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => await Set.AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await Set.AddAsync(entity, ct);

    public void Update(T entity) => Set.Update(entity);

    public void Delete(T entity) => Set.Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => SaveChangesGuard.SaveAsync(Context, ct);

    /// <summary>Специфікація перетворюється на один LINQ-вираз і ОДИН SQL-запит (WHERE, ORDER BY, OFFSET/FETCH).</summary>
    public virtual async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> specification, CancellationToken ct = default)
        => await Set.WithSpecification(specification).AsNoTracking().ToListAsync(ct);   // вибірки за специфікацією лише для читання

    public virtual Task<int> CountAsync(ISpecification<T> specification, CancellationToken ct = default)
        => Set.WithSpecification(specification).CountAsync(ct);
}
