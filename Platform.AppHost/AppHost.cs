using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Platform.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// ---- Паролі контейнерів: параметри-секрети AppHost (dotnet user-secrets), а не код. Потрібні стабільні паролі,
//      бо дані зберігаються у volume між перезапусками (Лаб. 05). ----
var sqlPassword = builder.AddParameter("sql-password", secret: true);
var mongoPassword = builder.AddParameter("mongo-password", secret: true);

// ---- Один SQL Server у контейнері для SQL-сервісів платформи (потрібен запущений Docker Desktop).
//      У ньому дві ОКРЕМІ бази: кожен сервіс має власну БД, спільних таблиць немає. WithDataVolume: дані переживають перезапуск. ----
var sql = builder.AddSqlServer("sql", sqlPassword).WithDataVolume();

// Сервіс «Рекомендації» (Лаб. 02): ім'я ресурсу збігається з GetConnectionString("RecommendationsDb").
var recommendationsDb = sql.AddDatabase("RecommendationsDb", "OutfitRecommendationsDb");

// Сервіс «Гардероб» (Лаб. 03, EF Core): ім'я ресурсу збігається з GetConnectionString("WardrobeDb").
// Схему створює сам сервіс при старті (міграції EF Core, потім ідемпотентний сідінг): скрипти не потрібні.
var wardrobeDb = sql.AddDatabase("WardrobeDb", "WardrobeEfDb");

// ---- MongoDB у контейнері для сервісу «Відгуки» (Лаб. 04: Clean Architecture + CQRS) ----
// Фіксований порт 27018, щоб не конфліктувати з локальною MongoDB на 27017 і зручно підключатись із Compass.
var mongo = builder.AddMongoDB("mongo", port: 27018, password: mongoPassword).WithDataVolume();

// Ім'я ресурсу «FeedbackDb» збігається з ConnectionStrings:FeedbackDb, який читає Feedback.Infrastructure.
// Індекси й початкові дані сервіс створює сам при старті (IndexCreationService, DataSeedingService).
var feedbackDb = mongo.AddDatabase("FeedbackDb", "outfitFeedbackDb");

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
var recommendations = builder.AddProject<Projects.Recommendations_Api>("recommendations-api")
    .WithReference(recommendationsDb)
    .WaitFor(recommendationsDb);

var wardrobe = builder.AddProject<Projects.Wardrobe_Api>("wardrobe-api")
    .WithReference(wardrobeDb)
    .WaitFor(wardrobeDb);

var feedback = builder.AddProject<Projects.Feedback_WebApi>("feedback-api")
    .WithReference(feedbackDb)
    .WaitFor(feedbackDb);

// ---- Лаб. 05: Aggregator опитує сервіси за логічними іменами (service discovery) ----
var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithReference(recommendations).WithReference(wardrobe).WithReference(feedback)
    .WaitFor(recommendations).WaitFor(wardrobe).WaitFor(feedback);

// ---- Лаб. 05: Gateway (YARP): єдина точка входу на фіксованому порту 5000 ----
builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "public")
    .WithExternalHttpEndpoints()
    .WithReference(recommendations).WithReference(wardrobe).WithReference(feedback)
    .WithReference(aggregator)
    .WaitFor(aggregator);

builder.Build().Run();
