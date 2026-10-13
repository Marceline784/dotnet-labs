namespace Recommendations.Domain.Models;

public class OutfitItem
{
    public long OutfitId { get; set; }
    public long WardrobeItemId { get; set; }
    public string SlotCode { get; set; } = "";
    public string Layer { get; set; } = "Base";
    public string ItemName { get; set; } = "";      
    public string? ImageUrl { get; set; }           
    public bool IsRemovable { get; set; }           // шар, який можна зняти протягом дня
}
