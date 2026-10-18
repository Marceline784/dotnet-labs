using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Platform.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// ---- База даних: SQL Server у контейнері (потрібен запущений Docker Desktop) ----
var sql = builder.AddSqlServer("sql");

// Ім'я ресурсу "RecommendationsDb" збігається з ім'ям рядка підключення в Recommendations.Api
// (GetConnectionString("RecommendationsDb")), тому Aspire передає рядок у API сам
// через змінну оточення ConnectionStrings__RecommendationsDb, і код API змінювати не треба.
// Друге значення, це реальна назва бази з Лаб. 01.
var db = sql.AddDatabase("RecommendationsDb", "OutfitRecommendationsDb");

// Контейнер стартує порожнім: застосовуємо скрипти з Лаб. 01 (db/p1). Скрипти ідемпотентні,
// тому повторний запуск не створює дублікатів. Обробник виконується, коли SQL Server готовий приймати з'єднання.
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

// ---- Сервіс: API стартує після того, як база готова ----
builder.AddProject<Projects.Recommendations_Api>("recommendations-api")
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();
