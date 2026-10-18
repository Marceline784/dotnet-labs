using Recommendations.Domain.Models;

namespace Recommendations.Bll.Generation;

public interface IOutfitGenerator
{
    /// <summary>
    /// Складає до <paramref name="count"/> образів із відібраних речей (слоти: верх із шарами, низ, взуття, сумка, аксесуари).
    /// Образи з відбитками з <paramref name="excludedHashes"/> (щойно одягнені) пропускаються.
    /// </summary>
    IReadOnlyList<Outfit> Generate(IReadOnlyList<WardrobeItem> eligible, WeatherSnapshot weather, UserProfile profile,
        int count, IReadOnlySet<string> excludedHashes);
}
