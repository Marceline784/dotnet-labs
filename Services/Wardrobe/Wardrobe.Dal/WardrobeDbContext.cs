using Microsoft.EntityFrameworkCore;
using Wardrobe.Domain.Entities;

namespace Wardrobe.Dal;

/// <summary>Контекст EF Core сервісу «Гардероб» (Code First).</summary>
public class WardrobeDbContext(DbContextOptions<WardrobeDbContext> options) : DbContext(options)
{
    public DbSet<WardrobeItem> WardrobeItems => Set<WardrobeItem>();
    public DbSet<ItemDetail> ItemDetails => Set<ItemDetail>();
    public DbSet<ItemImage> ItemImages => Set<ItemImage>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ItemCategory> ItemCategories => Set<ItemCategory>();

    /// <summary>Усі конфігурації (IEntityTypeConfiguration&lt;T&gt;) підключаються централізовано.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(WardrobeDbContext).Assembly);
}
