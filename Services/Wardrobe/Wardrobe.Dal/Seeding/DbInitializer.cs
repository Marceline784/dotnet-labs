using Microsoft.EntityFrameworkCore;
using Wardrobe.Domain.Entities;
using Wardrobe.Domain.Enums;

namespace Wardrobe.Dal.Seeding;

/// <summary>
/// ПРОГРАМНИЙ сідінг: виконується після MigrateAsync() і ідемпотентний: якщо дані вже є, нічого не робить.
/// Довідник категорій заповнюється окремо, статично (HasData в CategoryConfiguration).
/// У production вимикається (див. Program.cs).
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(WardrobeDbContext db, CancellationToken ct = default)
    {
        if (await db.WardrobeItems.AnyAsync(ct)) return;     // уже наповнено

        // спочатку довідники, потім залежні записи (цілісність FK)
        var categories = await db.Categories.ToDictionaryAsync(c => c.Name, ct);

        WardrobeItem Make(long owner, string name, Slot slot, Layer layer,
            byte warmth, short minTemp, short maxTemp, byte water, bool windproof,
            string material, string color, params string[] categoryNames)
        {
            var item = new WardrobeItem(owner, name, slot, layer);
            item.SetDetail(new ItemDetail(warmth, minTemp, maxTemp, water, windproof, material, color));
            item.AddImage($"/img/wardrobe/{name.ToLowerInvariant().Replace(' ', '-')}.jpg", makeMain: true);
            foreach (var categoryName in categoryNames)
                item.AddToCategory(categories[categoryName]);
            return item;
        }

        // Користувачі 1 (Олена) і 2 (Макс) збігаються з тестовими даними сервісу «Рекомендації».
        var items = new List<WardrobeItem>
        {
            Make(1, "Down puffer jacket",    Slot.Top,       Layer.Outer, 5, -30,  5, 2, true,  "Down/polyester",   "Black",     "Outerwear", "Winter"),
            Make(1, "Lightweight windbreaker", Slot.Top,     Layer.Outer, 2,   8, 20, 2, true,  "Nylon",            "Olive",     "Outerwear", "Demi-season"),
            Make(1, "Wool sweater",          Slot.Top,       Layer.Mid,   4, -10, 14, 0, false, "Wool",             "Beige",     "Sweaters & hoodies", "Demi-season", "Winter"),
            Make(1, "White t-shirt",         Slot.Top,       Layer.Base,  1, -20, 40, 0, false, "Cotton",           "White",     "T-shirts", "Summer", "Demi-season"),
            Make(1, "Thermal long sleeve",   Slot.Top,       Layer.Base,  3, -25, 10, 0, false, "Merino wool",      "Grey",      "T-shirts", "Winter"),
            Make(1, "Insulated jeans",       Slot.Bottom,    Layer.Base,  4, -15,  8, 0, false, "Fleece-lined denim", "Blue",    "Trousers", "Winter"),
            Make(1, "Classic jeans",         Slot.Bottom,    Layer.Base,  2,   0, 22, 0, false, "Denim",            "Blue",      "Trousers", "Demi-season"),
            Make(1, "Denim shorts",          Slot.Bottom,    Layer.Base,  1,  20, 40, 0, false, "Denim",            "Light blue", "Shorts", "Summer"),
            Make(1, "Winter boots",          Slot.Shoes,     Layer.Base,  5, -30,  8, 3, true,  "Leather/membrane", "Brown",     "Footwear", "Winter"),
            Make(1, "White sneakers",        Slot.Shoes,     Layer.Base,  2,   4, 30, 1, false, "Textile",          "White",     "Footwear", "Demi-season", "Summer"),
            Make(1, "Wool beanie",           Slot.Accessory, Layer.Base,  4, -30,  8, 0, true,  "Wool",             "Grey",      "Headwear", "Winter"),
            Make(1, "Canvas tote",           Slot.Bag,       Layer.Base,  1, -30, 40, 0, false, "Canvas",           "Beige",     "Bags"),
            Make(1, "Waterproof backpack",   Slot.Bag,       Layer.Base,  1, -30, 40, 2, false, "Nylon",            "Black",     "Bags"),
            Make(1, "Sunglasses",            Slot.Accessory, Layer.Base,  1,  12, 40, 0, false, "Plastic",          "Black",     "Summer"),
            Make(1, "Wool scarf",            Slot.Accessory, Layer.Base,  3, -30, 10, 0, false, "Wool",             "Red",       "Scarves", "Winter"),

            Make(2, "Parka",                 Slot.Top,       Layer.Outer, 5, -30,  3, 2, true,  "Polyester",        "Khaki",     "Outerwear", "Winter"),
            Make(2, "Hoodie",                Slot.Top,       Layer.Mid,   3,  -5, 16, 0, false, "Fleece",           "Grey",      "Sweaters & hoodies", "Demi-season"),
            Make(2, "Black t-shirt",         Slot.Top,       Layer.Base,  1, -20, 40, 0, false, "Cotton",           "Black",     "T-shirts"),
            Make(2, "Chinos",                Slot.Bottom,    Layer.Base,  2,   0, 25, 0, false, "Cotton",           "Beige",     "Trousers", "Demi-season"),
            Make(2, "Winter sneakers",       Slot.Shoes,     Layer.Base,  4, -20,  8, 2, true,  "Membrane",         "Black",     "Footwear", "Winter"),
            Make(2, "Baseball cap",          Slot.Accessory, Layer.Base,  1,  10, 40, 0, false, "Cotton",           "Black",     "Headwear", "Summer"),
        };

        db.WardrobeItems.AddRange(items);
        await db.SaveChangesAsync(ct);     // один виклик: усе або нічого (одна транзакція)
    }
}
