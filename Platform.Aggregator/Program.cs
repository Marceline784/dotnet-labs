using System.Text.Json.Serialization;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();   // Serilog, OpenTelemetry, CorrelationId, service discovery

// ---- Typed HttpClients: адреси за логічними іменами ресурсів AppHost; CorrelationId додає CorrelationIdHandler ----
builder.Services.AddHttpClient<RecommendationsClient>(c => c.BaseAddress = new("https+http://recommendations-api"));
builder.Services.AddHttpClient<WardrobeClient>(c => c.BaseAddress = new("https+http://wardrobe-api"));
builder.Services.AddHttpClient<FeedbackClient>(c => c.BaseAddress = new("https+http://feedback-api"));
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
