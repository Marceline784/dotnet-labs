using System.Security.Cryptography;
using System.Text;

namespace Recommendations.Bll.Generation;

/// <summary>
/// Відбиток образу. ОБОВ'ЯЗКОВО збігається з тим, що рахує БД в usp_AddOutfit:
/// SHA-256 від рядка «id1,id2,...» (Id за зростанням), у кодуванні UTF-16 (NVARCHAR), великий HEX.
/// </summary>
public static class OutfitHasher
{
    public static string Compute(IEnumerable<long> itemIds)
    {
        var csv = string.Join(",", itemIds.OrderBy(x => x));
        var bytes = SHA256.HashData(Encoding.Unicode.GetBytes(csv));   // Unicode = UTF-16LE, як NVARCHAR у SQL Server
        return Convert.ToHexString(bytes);                             // великі літери, як CONVERT(..., 2)
    }
}
