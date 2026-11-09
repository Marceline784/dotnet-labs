using System.Text.Json.Serialization;
using Platform.Aggregator.Caching;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Services;
using Wearcast.Contracts.Feedback;
using Wearcast.Contracts.Recommendations;
using Wearcast.Contracts.Wardrobe;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();   // Serilog, OpenTelemetry, CorrelationId, service discovery

// ---- Лаб. 06: gRPC-клієнти замість HttpClient. Адреса = логічне ім'я ресурсу AppHost (service discovery, https для HTTP/2);
//      resilience і CorrelationIdHandler з ServiceDefaults діють і тут (клієнти створює та сама фабрика HttpClient) ----
builder.Services.AddGrpcClient<RecommendationsService.RecommendationsServiceClient>(o => o.Address = new Uri("https://recommendations-api"));
builder.Services.AddGrpcClient<WardrobeService.WardrobeServiceClient>(o => o.Address = new Uri("https://wardrobe-api"));
builder.Services.AddGrpcClient<FeedbackService.FeedbackServiceClient>(o => o.Address = new Uri("https://feedback-api"));
builder.Services.AddScoped<RecommendationsClient>();
builder.Services.AddScoped<WardrobeClient>();
builder.Services.AddScoped<FeedbackClient>();

// ---- Redis: кеш агрегованих відповідей (TTL 30 с) ----
builder.AddRedisDistributedCache("redis");
builder.Services.AddSingleton<AggregatorMetrics>();
builder.Services.AddScoped<RecommendationCardService>();

// ---- Web API: camelCase, null-значення не серіалізуються ----
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ctx =>
{
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
    var correlationId = ctx.HttpContext.Request.Headers["X-Correlation-Id"].ToString();
    if (!string.IsNullOrEmpty(correlationId))
        ctx.ProblemDetails.Extensions["correlationId"] = correlationId;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.MapDefaultEndpoints();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
