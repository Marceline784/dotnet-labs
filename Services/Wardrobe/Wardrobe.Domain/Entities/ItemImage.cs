using Wardrobe.Domain.Exceptions;
using Wardrobe.Domain.Rules;

namespace Wardrobe.Domain.Entities;

/// <summary>Фото речі (зв'язок 1:N). Створюється й змінюється лише через WardrobeItem.</summary>
public class ItemImage
{
    public long Id { get; private set; }
    public long ItemId { get; private set; }
    public string Url { get; private set; } = "";
    public bool IsPrimary { get; internal set; }
    public int SortOrder { get; internal set; }

    private ItemImage() { }                             // для EF Core

    internal ItemImage(string url, bool isPrimary, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("Image url must not be empty");
        if (url.Length > ItemRules.UrlMaxLength)
            throw new DomainException($"Image url is too long (max {ItemRules.UrlMaxLength})");

        Url = url.Trim();
        IsPrimary = isPrimary;
        SortOrder = sortOrder;
    }
}
