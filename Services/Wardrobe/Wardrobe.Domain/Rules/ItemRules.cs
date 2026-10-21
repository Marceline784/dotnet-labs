namespace Wardrobe.Domain.Rules;

/// <summary>
/// Єдине місце обмежень предметної області. Ними користуються і сутності (перевірки),
/// і конфігурації EF Core (довжини стовпців), і валідатори: правила не дублюються.
/// </summary>
public static class ItemRules
{
    public const int NameMaxLength = 200;
    public const int UrlMaxLength = 500;
    public const int MaterialMaxLength = 100;
    public const int ColorMaxLength = 50;
    public const int NotesMaxLength = 500;
    public const int CategoryNameMaxLength = 100;

    public const int MaxImagesPerItem = 10;

    public const byte MinWarmth = 1;
    public const byte MaxWarmth = 5;
    public const byte MaxWaterResistance = 3;
}
