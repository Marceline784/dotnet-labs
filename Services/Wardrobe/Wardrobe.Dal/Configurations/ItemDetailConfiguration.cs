using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wardrobe.Domain.Entities;
using Wardrobe.Domain.Rules;

namespace Wardrobe.Dal.Configurations;

public class ItemDetailConfiguration : IEntityTypeConfiguration<ItemDetail>
{
    public void Configure(EntityTypeBuilder<ItemDetail> builder)
    {
        builder.ToTable("ItemDetails", t =>
        {
            t.HasCheckConstraint("CK_ItemDetails_Warmth",
                $"[Warmth] BETWEEN {ItemRules.MinWarmth} AND {ItemRules.MaxWarmth}");
            t.HasCheckConstraint("CK_ItemDetails_Water",
                $"[WaterResistance] BETWEEN 0 AND {ItemRules.MaxWaterResistance}");
            t.HasCheckConstraint("CK_ItemDetails_TempRange", "[MaxTemp] > [MinTemp]");
        });

        builder.HasKey(d => d.ItemId);                     // PK = FK (зв'язок 1:1)
        builder.Property(d => d.ItemId).ValueGeneratedNever();

        builder.Property(d => d.Material).HasMaxLength(ItemRules.MaterialMaxLength);
        builder.Property(d => d.Color).HasMaxLength(ItemRules.ColorMaxLength);
        builder.Property(d => d.Notes).HasMaxLength(ItemRules.NotesMaxLength);
        builder.Property(d => d.Warmth).IsRequired();
        builder.Property(d => d.MinTemp).IsRequired();
        builder.Property(d => d.MaxTemp).IsRequired();
        builder.Property(d => d.WaterResistance).IsRequired();
        builder.Property(d => d.IsWindproof).IsRequired();
    }
}
