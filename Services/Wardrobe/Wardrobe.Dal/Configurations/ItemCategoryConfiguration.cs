using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wardrobe.Domain.Entities;

namespace Wardrobe.Dal.Configurations;

/// <summary>Проміжна сутність M:N із власним полем AddedAt.</summary>
public class ItemCategoryConfiguration : IEntityTypeConfiguration<ItemCategory>
{
    public void Configure(EntityTypeBuilder<ItemCategory> builder)
    {
        builder.ToTable("ItemCategories");

        builder.HasKey(ic => new { ic.ItemId, ic.CategoryId });   // складений ключ

        builder.Property(ic => ic.AddedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(ic => ic.Item)
            .WithMany(i => i.ItemCategories)
            .HasForeignKey(ic => ic.ItemId)
            .OnDelete(DeleteBehavior.Cascade);                   // зв'язки зникають разом із річчю

        builder.HasOne(ic => ic.Category)
            .WithMany(c => c.ItemCategories)
            .HasForeignKey(ic => ic.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);                  // категорію з речами видалити не можна

        // індекс під зворотний пошук: «усі речі категорії»
        builder.HasIndex(ic => ic.CategoryId).HasDatabaseName("IX_ItemCategories_CategoryId");
    }
}
