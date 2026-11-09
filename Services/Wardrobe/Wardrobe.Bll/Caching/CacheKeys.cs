using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Wardrobe.Bll.Caching;

/// <summary>Передбачувані ключі з префіксом сервісу (wardrobe:...).</summary>
public static class CacheKeys
{
    public const string ItemsScope = "wardrobe:items-epoch";     // змінюється при перейменуванні/видаленні категорії
    public const string ListScope = "wardrobe:list";             // змінюється при будь-якому записі
    public const string CategoriesScope = "wardrobe:categories";

    public static string Item(long epoch, long id) => $"wardrobe:item:e{epoch}:{id}";
    public static string List(long version, object query) => $"{ListScope}:v{version}:{Hash(query)}";
    public static string Laundry(long version, long ownerUserId) => $"{ListScope}:v{version}:laundry:{ownerUserId}";
    public static string Categories(long version) => $"{CategoriesScope}:v{version}";

    private static string Hash(object query)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(query))))[..16].ToLowerInvariant();
}
