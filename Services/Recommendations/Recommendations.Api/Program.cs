using Recommendations.Api.Infrastructure;
using Recommendations.Bll.Generation;
using Recommendations.Bll.Mapping;
using Recommendations.Bll.Services;
using Recommendations.Bll.Weather;
using Recommendations.Dal;
using Recommendations.Dal.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ---- Підключення до БД: з appsettings.json, перевизначається змінною оточення ConnectionStrings__RecommendationsDb
//      (саме так передає рядок підключення .NET Aspire, коли ім'я ресурсу в AppHost збігається з цим) ----
var connectionString = builder.Configuration.GetConnectionString("RecommendationsDb")
    ?? throw new InvalidOperationException("Connection string 'RecommendationsDb' is missing");

// ---- DAL: Unit of Work (Scoped: один екземпляр і одне з'єднання на HTTP-запит) ----
builder.Services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));

// ---- BLL: сервіси, генератор образів ----
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IWardrobeService, WardrobeService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IOutfitService, OutfitService>();
builder.Services.AddSingleton<IOutfitGenerator, OutfitGenerator>();

// ---- Погода: реальний прогноз Open-Meteo або Fake (Weather:Provider = Fake) для демо без інтернету ----
if (string.Equals(builder.Configuration["Weather:Provider"], "Fake", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IWeatherProvider, FakeWeatherProvider>();
}
else
{
    builder.Services.AddHttpClient<IWeatherProvider, OpenMeteoWeatherProvider>(client =>
    {
        client.BaseAddress = new Uri("https://api.open-meteo.com/");
        client.Timeout = TimeSpan.FromSeconds(10);
    });
}

// ---- AutoMapper: профілі зібрані в BLL ----
builder.Services.AddAutoMapper(cfg =>
{
    cfg.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    cfg.AddProfile<MappingProfile>();
});

// ---- Web API: тонкі контролери; помилки у форматі ProblemDetails (RFC 9457) ----
builder.Services.AddControllers();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ctx =>
{
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
    if (ctx.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var correlationId))
        ctx.ProblemDetails.Extensions["correlationId"] = correlationId;
});
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// ---- OpenAPI / Swagger ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();   // винятки -> ProblemDetails (DomainExceptionHandler)
app.UseStatusCodePages();    // порожні 404/405 тощо -> ProblemDetails

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();

// потрібно для інтеграційних тестів (WebApplicationFactory<Program>)
public partial class Program { }
