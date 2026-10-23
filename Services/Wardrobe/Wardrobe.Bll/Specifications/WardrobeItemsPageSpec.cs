using Ardalis.Specification;
using Wardrobe.Bll.Dtos;

namespace Wardrobe.Bll.Specifications;

/// <summary>
/// Специфікація СТОРІНКИ: ті самі критерії + Include + сортування + Skip/Take.
/// Усе перетворюється в ОДИН SQL-запит (WHERE ... ORDER BY ... OFFSET ... FETCH),
/// до пам'яті потрапляє лише потрібна сторінка.
/// </summary>
public class WardrobeItemsPageSpec : WardrobeItemsFilterSpec
{
    public WardrobeItemsPageSpec(WardrobeItemQuery q, int page, int pageSize) : base(q)
    {
        Query.Include(i => i.Detail)
             .Include(i => i.Images);

        var desc = string.Equals(q.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

        // лише дозволені поля сортування; Id наприкінці робить порядок стабільним для пагінації
        switch (q.SortBy?.ToLowerInvariant())
        {
            case "slot":
                if (desc) Query.OrderByDescending(i => i.Slot).ThenBy(i => i.Name).ThenBy(i => i.Id);
                else Query.OrderBy(i => i.Slot).ThenBy(i => i.Name).ThenBy(i => i.Id);
                break;
            case "created":
                if (desc) Query.OrderByDescending(i => i.CreatedAt).ThenBy(i => i.Id);
                else Query.OrderBy(i => i.CreatedAt).ThenBy(i => i.Id);
                break;
            default:
                if (desc) Query.OrderByDescending(i => i.Name).ThenBy(i => i.Id);
                else Query.OrderBy(i => i.Name).ThenBy(i => i.Id);
                break;
        }

        Query.Skip((page - 1) * pageSize).Take(pageSize);
    }
}
