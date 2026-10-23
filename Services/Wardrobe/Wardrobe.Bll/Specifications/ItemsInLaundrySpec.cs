using Ardalis.Specification;
using Wardrobe.Domain.Entities;

namespace Wardrobe.Bll.Specifications;

/// <summary>
/// ДРУГА специфікація з іншими критеріями: речі користувача, які зараз у пранні.
/// Застосовується через той самий ListAsync/WithSpecification без змін у репозиторії.
/// </summary>
public class ItemsInLaundrySpec : Specification<WardrobeItem>
{
    public ItemsInLaundrySpec(long ownerUserId)
    {
        Query.Where(i => i.OwnerUserId == ownerUserId && !i.IsDeleted && !i.IsAvailable)
             .Include(i => i.Detail)
             .Include(i => i.Images)
             .OrderBy(i => i.Name).ThenBy(i => i.Id);
    }
}
