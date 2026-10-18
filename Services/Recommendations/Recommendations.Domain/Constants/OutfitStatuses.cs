namespace Recommendations.Domain.Constants;

public static class OutfitStatuses
{
    public const string Generated = "Generated";
    public const string Chosen = "Chosen";
    public const string Worn = "Worn";
    public const string Reviewed = "Reviewed";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All =
        new[] { Generated, Chosen, Worn, Reviewed, Cancelled };
}
