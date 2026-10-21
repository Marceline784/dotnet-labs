using Wardrobe.Domain.Enums;
using Wardrobe.Domain.Exceptions;
using Wardrobe.Domain.Rules;

namespace Wardrobe.Domain.Entities;

/// <summary>
/// Річ гардероба: корінь агрегату (WardrobeItem + ItemDetail + ItemImage + ItemCategory).
/// Бізнес-правила реалізовані МЕТОДАМИ сутності, а не відкритими setter-ами:
/// некоректний стан створити неможливо (аналог CatalogItem.RemoveStock в eShop).
/// </summary>
public class WardrobeItem
{
    private readonly List<ItemImage> _images = new();
    private readonly List<ItemCategory> _itemCategories = new();

    public long Id { get; private set; }
    public long OwnerUserId { get; private set; }       // посилання на користувача з іншого сервісу, БЕЗ FK
    public string Name { get; private set; } = "";
    public Slot Slot { get; private set; }
    public Layer Layer { get; private set; }
    public bool IsAvailable { get; private set; } = true;   // false = у пранні
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }             // м'яке видалення
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();   // оптимістична конкурентність

    public ItemDetail? Detail { get; private set; }                         // 1:1
    public IReadOnlyCollection<ItemImage> Images => _images;                // 1:N
    public IReadOnlyCollection<ItemCategory> ItemCategories => _itemCategories;   // M:N

    private WardrobeItem() { }                          // для EF Core

    public WardrobeItem(long ownerUserId, string name, Slot slot, Layer layer)
    {
        if (ownerUserId <= 0)
            throw new DomainException("Owner id must be positive");

        Name = ValidateName(name);
        EnsureLayerAllowed(slot, layer);

        OwnerUserId = ownerUserId;
        Slot = slot;
        Layer = layer;
        CreatedAt = DateTime.UtcNow;
    }

    // ---------- основні дані ----------

    public void Update(string name, Slot slot, Layer layer)
    {
        var validName = ValidateName(name);
        EnsureLayerAllowed(slot, layer);

        Name = validName;
        Slot = slot;
        Layer = layer;
        Touch();
    }

    public void MarkInLaundry()
    {
        if (!IsAvailable)
            throw new DomainException("The item is already in the laundry");
        IsAvailable = false;
        Touch();
    }

    public void ReturnFromLaundry()
    {
        if (IsAvailable)
            throw new DomainException("The item is not in the laundry");
        IsAvailable = true;
        Touch();
    }

    public void Delete()
    {
        IsDeleted = true;
        Touch();
    }

    // ---------- 1:1 деталі ----------

    public void SetDetail(ItemDetail detail)
    {
        Detail = detail ?? throw new DomainException("Detail must not be null");
        Touch();
    }

    // ---------- 1:N фото ----------

    public ItemImage AddImage(string url, bool makeMain = false)
    {
        if (_images.Count >= ItemRules.MaxImagesPerItem)
            throw new DomainException($"An item can have at most {ItemRules.MaxImagesPerItem} images");

        var isPrimary = makeMain || _images.Count == 0;      // перше фото автоматично головне
        if (isPrimary)
            foreach (var existing in _images) existing.IsPrimary = false;

        var image = new ItemImage(url, isPrimary, _images.Count);
        _images.Add(image);
        Touch();
        return image;
    }

    public void SetPrimaryImage(long imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId)
            ?? throw new EntityNotFoundException(nameof(ItemImage), imageId);

        foreach (var existing in _images) existing.IsPrimary = false;
        image.IsPrimary = true;
        Touch();
    }

    public void RemoveImage(long imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId)
            ?? throw new EntityNotFoundException(nameof(ItemImage), imageId);

        _images.Remove(image);
        if (image.IsPrimary && _images.Count > 0)
            _images[0].IsPrimary = true;                      // головне фото завжди існує, якщо є фото
        Touch();
    }

    // ---------- M:N категорії ----------

    public void AddToCategory(Category category)
    {
        if (category is null)
            throw new DomainException("Category must not be null");
        if (_itemCategories.Any(ic => ic.CategoryId == category.Id && category.Id != 0
                                      || ReferenceEquals(ic.Category, category)))
            throw new DomainException($"The item is already in category '{category.Name}'");

        _itemCategories.Add(new ItemCategory(this, category));
        Touch();
    }

    public void RemoveFromCategory(long categoryId)
    {
        var link = _itemCategories.FirstOrDefault(ic => ic.CategoryId == categoryId)
            ?? throw new EntityNotFoundException("Item category link", categoryId);

        _itemCategories.Remove(link);
        Touch();
    }

    // ---------- внутрішні перевірки ----------

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Item name must not be empty");
        name = name.Trim();
        if (name.Length > ItemRules.NameMaxLength)
            throw new DomainException($"Item name is too long (max {ItemRules.NameMaxLength})");
        return name;
    }

    /// <summary>Шари (Mid, Outer) дозволені лише для верху: правило з БД Лаб. 01 тепер живе в сутності.</summary>
    private static void EnsureLayerAllowed(Slot slot, Layer layer)
    {
        if (slot != Slot.Top && layer != Layer.Base)
            throw new DomainException("Layers other than Base are allowed only for the Top slot");
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
