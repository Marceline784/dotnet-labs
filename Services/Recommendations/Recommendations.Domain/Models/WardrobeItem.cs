namespace Recommendations.Domain.Models;

public class WardrobeItem
{
    public long Id { get; set; }
    public long OwnerUserId { get; set; }
    public string Name { get; set; } = "";
    public string SlotCode { get; set; } = "";      // Top | Bottom | Shoes | Bag | Accessory
    public string Layer { get; set; } = "Base";     
    public byte Warmth { get; set; }                
    public short MinTemp { get; set; }
    public short MaxTemp { get; set; }
    public byte WaterResistance { get; set; }       // 0..3
    public bool IsAvailable { get; set; } = true;   // false = у пранні
    public string? PrimaryImageUrl { get; set; }
    public DateTime SourceUpdatedAt { get; set; }
}
