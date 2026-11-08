using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Wardrobe.Api.Infrastructure;
using Wardrobe.Bll.Mapping;
using Wardrobe.Bll.Services;
using Wardrobe.Bll.Validators;
using Wardrobe.Dal;
using Wardrobe.Dal.Interfaces;
using Wardrobe.Dal.Seeding;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();   // Лаб. 05: Serilog, OpenTelemetry, health checks, service discovery, CorrelationId

// ---- База даних: рядок підключення з appsettings.json, перевизначається змінною ConnectionStrings__WardrobeDb
//      (її передає .NET Aspire, якщо ім'я ресурсу в AppHost збігається) ----
var connectionString = builder.Configuration.GetConnectionString("WardrobeDb")
    ?? throw new InvalidOperationException("Connection string 'WardrobeDb' is missing");

builder.Services.AddDbContext<WardrobeDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    if (builder.Environment.IsDevelopment())
        options.EnableSensitiveDataLogging();   // значення параметрів у SQL-логах (лише для розробки)
});
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ---- BLL ----
builder.Services.AddScoped<IWardrobeItemService, WardrobeItemService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddAutoMapper(cfg =>
{
    cfg.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    cfg.AddProfile<MappingProfile>();
});
builder.Services.AddValidatorsFromAssemblyContaining<CreateWardrobeItemDtoValidator>();

// ---- Web API ----
builder.Services.AddControllers();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ctx =>
{
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
    if (ctx.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var correlationId))
        ctx.ProblemDetails.Extensions["correlationId"] = correlationId;
});
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.MapDefaultEndpoints();      // Лаб. 05: /health і /alive

// ---- Міграції та сідінг при старті (сідінг вимикається в production або Database:Seed = false) ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WardrobeDbContext>();
    await db.Database.MigrateAsync();

    var seedEnabled = app.Configuration.GetValue("Database:Seed", true);
    if (seedEnabled && !app.Environment.IsProduction())
        await DbInitializer.SeedAsync(db);
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();     // один структурований запис на запит: метод, шлях, статус, тривалість
app.UseExceptionHandler();          // винятки -> ProblemDetails (ApiExceptionHandler)
app.UseStatusCodePages();           // порожні 404/405 -> ProblemDetails

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();

// потрібно для інтеграційних тестів (WebApplicationFactory<Program>)
public partial class Program { }
