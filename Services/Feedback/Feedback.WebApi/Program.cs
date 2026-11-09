using Feedback.Application;
using Feedback.Application.Caching;
using Feedback.Infrastructure;
using Feedback.WebApi.ErrorHandling;
using Feedback.WebApi.GrpcServices;
using Feedback.WebApi.Mapping;
using Feedback.WebApi.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();   // Лаб. 05: Serilog, OpenTelemetry, health checks, service discovery, CorrelationId

// ---- Кожен шар надає власний метод розширення ----
builder.Services
    .AddApplication(builder.Configuration)          // MediatR, Pipeline Behaviors, валідатори
    .AddInfrastructure(builder.Configuration);      // MongoDB: контекст, BSON-мапінг, репозиторії, індекси, сідінг

builder.AddFeedbackCaching();            // Лаб. 06: L1 (IMemoryCache) + L2 (Redis) + CachingBehavior (після AddApplication)
builder.Services.AddFeedbackGrpc();      // Лаб. 06

builder.Services.AddAutoMapper(cfg =>
{
    cfg.LicenseKey = builder.Configuration["LuckyPenny:LicenseKey"];
    cfg.AddProfile<FeedbackProfile>();
});

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

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();     // один структурований запис на запит
app.UseExceptionHandler();          // винятки -> ProblemDetails (ApiExceptionHandler)
app.UseStatusCodePages();           // порожні 404/405 -> ProblemDetails

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapFeedbackGrpc();      // Лаб. 06: gRPC-ендпоінти
app.Run();

// потрібно для інтеграційних тестів (WebApplicationFactory<Program>)
public partial class Program { }
