using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Wardrobe.Domain.Exceptions;

namespace Wardrobe.Dal;

/// <summary>
/// Перетворює помилки збереження EF Core на доменні винятки, щоб BLL та API не знали про EF і SQL:
/// конкурентна зміна (rowversion) та порушення UNIQUE (2601, 2627) -> ConflictException -> HTTP 409.
/// </summary>
internal static class SaveChangesGuard
{
    public static async Task<int> SaveAsync(DbContext context, CancellationToken ct)
    {
        try
        {
            return await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The record was modified by another request. Reload it and try again");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ConflictException("A record with the same unique value already exists");
        }
    }
}
