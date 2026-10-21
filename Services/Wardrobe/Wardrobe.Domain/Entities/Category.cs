using Wardrobe.Domain.Exceptions;
using Wardrobe.Domain.Rules;

namespace Wardrobe.Domain.Entities;

/// <summary>Довідник категорій (тип речі та сезон: «Верхній одяг», «Зима» тощо).</summary>
public class Category
{
    private readonly List<ItemCategory> _itemCategories = new();

    public long Id { get; private set; }
    public string Name { get; private set; } = "";
    public IReadOnlyCollection<ItemCategory> ItemCategories => _itemCategories;

    private Category() { }                              // для EF Core

    public Category(string name)
    {
        Name = ValidateName(name);
    }

    public void Rename(string name) => Name = ValidateName(name);

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name must not be empty");
        name = name.Trim();
        if (name.Length > ItemRules.CategoryNameMaxLength)
            throw new DomainException($"Category name is too long (max {ItemRules.CategoryNameMaxLength})");
        return name;
    }
}
