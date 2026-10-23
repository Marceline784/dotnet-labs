namespace Wardrobe.Bll.Common;

/// <summary>Розбір назви enum із рядка (без урахування регістру). Числа на кшталт «99» не приймаються.</summary>
public static class EnumParser
{
    public static bool TryParse<TEnum>(string? value, out TEnum result) where TEnum : struct, Enum
    {
        result = default;
        return !string.IsNullOrWhiteSpace(value)
               && !int.TryParse(value, out _)
               && Enum.TryParse(value, ignoreCase: true, out result)
               && Enum.IsDefined(result);
    }
}
