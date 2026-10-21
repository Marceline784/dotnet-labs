using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Wardrobe.Dal;

/// <summary>
/// Потрібна лише інструментам dotnet-ef (міграції): вони створюють контекст без запуску застосунку.
/// Рядок підключення береться зі змінної оточення WARDROBE_CONNECTION, інакше використовується значення за замовчуванням.
/// Самі застосунок і Aspire передають рядок через ConnectionStrings:WardrobeDb (див. Program.cs).
/// </summary>
public class WardrobeDbContextFactory : IDesignTimeDbContextFactory<WardrobeDbContext>
{
    private const string DefaultConnection =
        "Server=localhost\\SQLEXPRESS;Database=WardrobeEfDb;Trusted_Connection=True;TrustServerCertificate=True";

    public WardrobeDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("WARDROBE_CONNECTION") ?? DefaultConnection;

        var options = new DbContextOptionsBuilder<WardrobeDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new WardrobeDbContext(options);
    }
}
