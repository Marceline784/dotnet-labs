using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wardrobe.Domain.Entities;
using Wardrobe.Domain.Rules;

namespace Wardrobe.Dal.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.Name).IsRequired().HasMaxLength(ItemRules.CategoryNameMaxLength);
        builder.HasIndex(c => c.Name).IsUnique();          // назва категорії унікальна

        builder.Navigation(c => c.ItemCategories).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Статичний (модельний) сідінг довідника з фіксованими Id: потрапляє в міграцію, тому ідемпотентний.
        // Анонімні об'єкти, бо сутність має приватні сетери.
        builder.HasData(
            new { Id = 1L, Name = "Outerwear" },
            new { Id = 2L, Name = "Sweaters & hoodies" },
            new { Id = 3L, Name = "T-shirts" },
            new { Id = 4L, Name = "Trousers" },
            new { Id = 5L, Name = "Shorts" },
            new { Id = 6L, Name = "Footwear" },
            new { Id = 7L, Name = "Bags" },
            new { Id = 8L, Name = "Headwear" },
            new { Id = 9L, Name = "Scarves" },
            new { Id = 10L, Name = "Winter" },
            new { Id = 11L, Name = "Demi-season" },
            new { Id = 12L, Name = "Summer" });
    }
}
