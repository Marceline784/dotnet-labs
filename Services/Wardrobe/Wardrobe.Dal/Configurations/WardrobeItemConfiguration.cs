using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wardrobe.Domain.Entities;
using Wardrobe.Domain.Rules;

namespace Wardrobe.Dal.Configurations;

public class WardrobeItemConfiguration : IEntityTypeConfiguration<WardrobeItem>
{
    public void Configure(EntityTypeBuilder<WardrobeItem> builder)
    {
        builder.ToTable("WardrobeItems", t =>
        {
            t.HasCheckConstraint("CK_WardrobeItems_Slot",
                "[Slot] IN ('Top', 'Bottom', 'Shoes', 'Bag', 'Accessory')");
            t.HasCheckConstraint("CK_WardrobeItems_Layer",
                "[Layer] IN ('Base', 'Mid', 'Outer')");
            // шари Mid/Outer лише для верху (правило з Лаб. 01, продубльоване й у сутності)
            t.HasCheckConstraint("CK_WardrobeItems_LayerSlot",
                "[Slot] = 'Top' OR [Layer] = 'Base'");
        });

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedOnAdd();

        // OwnerUserId: посилання на користувача з іншого сервісу, БЕЗ зовнішнього ключа
        builder.Property(i => i.OwnerUserId).IsRequired();

        builder.Property(i => i.Name).IsRequired().HasMaxLength(ItemRules.NameMaxLength);

        // enum зберігається рядком (читабельно в БД, зручно для CHECK)
        builder.Property(i => i.Slot).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(i => i.Layer).HasConversion<string>().HasMaxLength(10).IsRequired();

        builder.Property(i => i.IsAvailable).IsRequired();
        builder.Property(i => i.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(i => i.UpdatedAt).HasColumnType("datetime2");
        builder.Property(i => i.IsDeleted).IsRequired();

        // оптимістична конкурентність: rowversion
        builder.Property(i => i.RowVersion).IsRowVersion();

        // індекс під «гардероб користувача за слотом» (покриваючий, фільтрований для «живих» записів)
        builder.HasIndex(i => new { i.OwnerUserId, i.Slot })
            .HasDatabaseName("IX_WardrobeItems_Owner_Slot")
            .HasFilter("[IsDeleted] = 0")
            .IncludeProperties(i => new { i.Name, i.IsAvailable });

        builder.HasIndex(i => i.Name);

        // 1:1 WardrobeItem <-> ItemDetail (FK є водночас PK залежної таблиці)
        builder.HasOne(i => i.Detail)
            .WithOne()
            .HasForeignKey<ItemDetail>(d => d.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // 1:N WardrobeItem -> ItemImage
        builder.HasMany(i => i.Images)
            .WithOne()
            .HasForeignKey(img => img.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // колекції доступні лише для читання ззовні, EF працює через приватні поля
        builder.Navigation(i => i.Images).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(i => i.ItemCategories).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
