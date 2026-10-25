using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Platform.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// ---- Один SQL Server у контейнері на всю платформу (потрібен запущений Docker Desktop).
//      У ньому дві ОКРЕМІ бази: кожен сервіс має власну БД, спільних таблиць немає. ----
var sql = builder.AddSqlServer("sql");

// Сервіс «Рекомендації» (Лаб. 02): ім'я ресурсу збігається з GetConnectionString("RecommendationsDb").
var recommendationsDb = sql.AddDatabase("RecommendationsDb", "OutfitRecommendationsDb");

// Сервіс «Гардероб» (Лаб. 03, EF Core): ім'я ресурсу збігається з GetConnectionString("WardrobeDb").
// Схему створює сам сервіс при старті (міграції EF Core, потім ідемпотентний сідінг): скрипти не потрібні.
var wardrobeDb = sql.AddDatabase("WardrobeDb", "WardrobeEfDb");

// Контейнер стартує порожнім: для «Рекомендацій» застосовуємо скрипти з Лаб. 01 (db/p1).
// Скрипти ідемпотентні; обробник виконується, коли SQL Server готовий приймати з'єднання.
var scriptsDir = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "db", "p1"));
builder.Eventing.Subscribe<ResourceReadyEvent>(sql.Resource, async (@event, ct) =>
{
    var logger = @event.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInit");
    try
    {
        var connectionString = await sql.Resource.GetConnectionStringAsync(ct)
            ?? throw new InvalidOperationException("SQL Server connection string is not available");

        await DatabaseInitializer.RunAsync(
            connectionString,
            new[]
            {
                Path.Combine(scriptsDir, "schema.sql"),
                Path.Combine(scriptsDir, "procedures.sql"),
                Path.Combine(scriptsDir, "seed.sql")
            },
            logger,
            ct);
    }
    catch (Exception ex)
    {
        // помилка ініціалізації не повинна валити весь AppHost: покажемо її в логах і дашборді
        logger.LogError(ex, "Database initialization failed");
    }
});

// ---- Сервіси: кожен стартує після того, як його база готова ----
builder.AddProject<Projects.Recommendations_Api>("recommendations-api")
    .WithReference(recommendationsDb)
    .WaitFor(recommendationsDb);

builder.AddProject<Projects.Wardrobe_Api>("wardrobe-api")
    .WithReference(wardrobeDb)
    .WaitFor(wardrobeDb);

builder.Build().Run();
