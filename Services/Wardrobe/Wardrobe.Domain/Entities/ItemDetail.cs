using Wardrobe.Domain.Exceptions;
using Wardrobe.Domain.Rules;

namespace Wardrobe.Domain.Entities;

/// <summary>
/// Характеристики речі (зв'язок 1:1 з WardrobeItem: ItemId є одночасно PK і FK).
/// Ці дані потрібні сервісу «Рекомендації» для підбору речей під погоду.
/// </summary>
public class ItemDetail
{
    public long ItemId { get; private set; }
    public string? Material { get; private set; }
    public string? Color { get; private set; }
    public byte Warmth { get; private set; }            // 1..5
    public short MinTemp { get; private set; }          // нижня межа комфорту, °C
    public short MaxTemp { get; private set; }          // верхня межа комфорту, °C
    public byte WaterResistance { get; private set; }   // 0..3
    public bool IsWindproof { get; private set; }
    public string? Notes { get; private set; }

    private ItemDetail() { }                            // для EF Core

    public ItemDetail(byte warmth, short minTemp, short maxTemp, byte waterResistance,
        bool isWindproof, string? material = null, string? color = null, string? notes = null)
    {
        Apply(warmth, minTemp, maxTemp, waterResistance, isWindproof, material, color, notes);
    }

    public void Update(byte warmth, short minTemp, short maxTemp, byte waterResistance,
        bool isWindproof, string? material, string? color, string? notes)
    {
        Apply(warmth, minTemp, maxTemp, waterResistance, isWindproof, material, color, notes);
    }

    private void Apply(byte warmth, short minTemp, short maxTemp, byte waterResistance,
        bool isWindproof, string? material, string? color, string? notes)
    {
        if (warmth < ItemRules.MinWarmth || warmth > ItemRules.MaxWarmth)
            throw new DomainException($"Warmth must be between {ItemRules.MinWarmth} and {ItemRules.MaxWarmth}");
        if (waterResistance > ItemRules.MaxWaterResistance)
            throw new DomainException($"Water resistance must be between 0 and {ItemRules.MaxWaterResistance}");
        if (maxTemp <= minTemp)
            throw new DomainException("MaxTemp must be greater than MinTemp");
        if (material?.Length > ItemRules.MaterialMaxLength)
            throw new DomainException($"Material is too long (max {ItemRules.MaterialMaxLength})");
        if (color?.Length > ItemRules.ColorMaxLength)
            throw new DomainException($"Color is too long (max {ItemRules.ColorMaxLength})");
        if (notes?.Length > ItemRules.NotesMaxLength)
            throw new DomainException($"Notes are too long (max {ItemRules.NotesMaxLength})");

        Warmth = warmth;
        MinTemp = minTemp;
        MaxTemp = maxTemp;
        WaterResistance = waterResistance;
        IsWindproof = isWindproof;
        Material = material?.Trim();
        Color = color?.Trim();
        Notes = notes?.Trim();
    }
}
