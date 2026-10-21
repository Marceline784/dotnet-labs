using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wardrobe.Domain.Entities;
using Wardrobe.Domain.Rules;

namespace Wardrobe.Dal.Configurations;

public class ItemImageConfiguration : IEntityTypeConfiguration<ItemImage>
{
    public void Configure(EntityTypeBuilder<ItemImage> builder)
    {
        builder.ToTable("ItemImages");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedOnAdd();

        builder.Property(i => i.Url).IsRequired().HasMaxLength(ItemRules.UrlMaxLength);
        builder.Property(i => i.IsPrimary).IsRequired();
        builder.Property(i => i.SortOrder).IsRequired();

        // індекс під «усі фото речі в порядку показу»
        builder.HasIndex(i => new { i.ItemId, i.SortOrder }).HasDatabaseName("IX_ItemImages_Item_Sort");
    }
}
