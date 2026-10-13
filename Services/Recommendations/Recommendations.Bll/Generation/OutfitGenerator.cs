using Recommendations.Domain.Constants;
using Recommendations.Domain.Models;

namespace Recommendations.Bll.Generation;

/// <summary>
/// Чиста бізнес-логіка підбору (без БД та HTTP), тому легко тестується.
/// Алгоритм:
///  1) ефективна температура = «відчувається як» - ColdSensitivity * 2 (як у БД);
///  2) шари верху: базовий завжди; середній, коли ефективний мінімум нижче 8; верхній, коли нижче 14 або опади;
///  3) для кожного слота беруться кращі за відповідністю температурі речі;
///  4) з них складаються комбінації; оцінка = 60 + 40 * середня відповідність ключових речей;
///  5) обираються найкращі образи з поправкою на різноманітність (щоб карусель не складалась із майже однакових).
/// </summary>
public sealed class OutfitGenerator : IOutfitGenerator
{
    private const decimal DiversityPenaltyPerSharedItem = 4m;

    public IReadOnlyList<Outfit> Generate(IReadOnlyList<WardrobeItem> eligible, WeatherSnapshot weather,
        UserProfile profile, int count, IReadOnlySet<string> excludedHashes)
    {
        if (count < 1 || eligible.Count == 0) return Array.Empty<Outfit>();

        var shift = profile.ColdSensitivity * 2m;
        var effMin = weather.FeelsLikeMin - shift;
        var effMax = weather.FeelsLikeMax - shift;
        var effMid = (effMin + effMax) / 2m;

        var rainy = weather.PrecipProbability >= 50 || weather.Condition is "Rain" or "Snow" or "Storm";
        var needOuter = effMin < 14m || rainy;
        var needMid = effMin < 8m;

        List<WardrobeItem> Pick(string slot, string? layer, int take) => eligible
            .Where(i => i.SlotCode == slot && (layer is null || i.Layer == layer))
            .OrderByDescending(i => Fit(i, effMid)).ThenBy(i => i.Id)
            .Take(take).ToList();

        var bases = Pick(SlotCodes.Top, "Base", 2);
        var mids = needMid ? Pick(SlotCodes.Top, "Mid", 2) : new List<WardrobeItem>();
        var outers = needOuter ? Pick(SlotCodes.Top, "Outer", 2) : new List<WardrobeItem>();

        if (bases.Count == 0)
        {
            // немає базового шару: беремо найкращу верхню річ будь-якого шару без додаткових шарів
            bases = Pick(SlotCodes.Top, null, 2);
            mids.Clear();
            outers.Clear();
        }

        var bottoms = Pick(SlotCodes.Bottom, null, 3);
        if (bases.Count == 0 || bottoms.Count == 0) return Array.Empty<Outfit>();   // без верху чи низу образу немає

        var shoes = Options(Pick(SlotCodes.Shoes, null, 2));
        var bags = Options(Pick(SlotCodes.Bag, null, 2));
        var accessories = Options(Pick(SlotCodes.Accessory, null, 2));

        var combos = new List<Combo>();
        foreach (var b in bases)
        foreach (var m in Options(mids))
        foreach (var o in Options(outers))
        foreach (var bt in bottoms)
        foreach (var s in shoes)
        foreach (var bg in bags)
        foreach (var ac in accessories)
        {
            var items = new[] { b, m, o, bt, s, bg, ac }.Where(x => x is not null).Select(x => x!).ToList();
            var hash = OutfitHasher.Compute(items.Select(i => i.Id));
            if (excludedHashes.Contains(hash)) continue;

            var core = items.Where(i => i.SlotCode is SlotCodes.Top or SlotCodes.Bottom or SlotCodes.Shoes).ToList();
            var score = Math.Round(60m + 40m * core.Average(i => Fit(i, effMid)), 2);
            combos.Add(new Combo(items, score, hash));
        }

        // жадібний вибір: найкращий за оцінкою, але зі штрафом за збіг речей з уже обраними образами
        var chosen = new List<Combo>();
        while (chosen.Count < count && combos.Count > 0)
        {
            var best = combos
                .OrderByDescending(c => c.Score - DiversityPenaltyPerSharedItem *
                    chosen.Sum(ch => c.Items.Select(i => i.Id).Intersect(ch.Items.Select(i => i.Id)).Count()))
                .ThenByDescending(c => c.Score)
                .First();
            chosen.Add(best);
            combos.Remove(best);
        }

        return chosen.Select(c => new Outfit
        {
            FitScore = c.Score,
            ItemsHash = c.Hash,
            Items = c.Items.Select(i => new OutfitItem
            {
                WardrobeItemId = i.Id,
                SlotCode = i.SlotCode,
                Layer = i.Layer,
                ItemName = i.Name,
                ImageUrl = i.PrimaryImageUrl,
                IsRemovable = i.SlotCode == SlotCodes.Top && i.Layer != "Base"
            }).ToList()
        }).ToList();
    }

    /// <summary>Наскільки річ підходить під температуру: 1, якщо діапазон речі містить її із запасом 10 градусів.</summary>
    private static decimal Fit(WardrobeItem i, decimal effMid)
    {
        var margin = Math.Min(effMid - i.MinTemp, i.MaxTemp - effMid);
        return Math.Clamp(margin / 10m, 0m, 1m);
    }

    private static List<WardrobeItem?> Options(List<WardrobeItem> list) =>
        list.Count == 0 ? new List<WardrobeItem?> { null } : list.Cast<WardrobeItem?>().ToList();

    private sealed record Combo(List<WardrobeItem> Items, decimal Score, string Hash);
}
