namespace Recommendations.Domain.Constants;

public static class SlotCodes
{
    public const string Top = "Top";
    public const string Bottom = "Bottom";
    public const string Shoes = "Shoes";
    public const string Bag = "Bag";
    public const string Accessory = "Accessory";

    public static readonly IReadOnlyList<string> All =
        new[] { Top, Bottom, Shoes, Bag, Accessory };
}
