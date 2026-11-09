using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Platform.ServiceDefaults;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Microsoft.Extensions.Hosting;

/// <summary>Спільні налаштування платформи (Лаб. 05): Serilog, OpenTelemetry, health checks, service discovery, CorrelationId.</summary>
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.AddSerilogLogging();
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<CorrelationIdHandler>();
        // CorrelationIdMiddleware ставиться на початок конвеєра автоматично: Program.cs сервісів міняти не треба
        builder.Services.AddTransient<IStartupFilter, CorrelationIdStartupFilter>();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler(o =>
            {
                // швидка деградація для демо: недоступний сервіс виявляється за ~1–2 с, а не за десятки секунд
                o.Retry.MaxRetryAttempts = 2;
                o.Retry.Delay = TimeSpan.FromMilliseconds(300);
                o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
                o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
                o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);
            });
            http.AddServiceDiscovery();
            http.AddHttpMessageHandler<CorrelationIdHandler>();
        });

        return builder;
    }

    private static TBuilder AddSerilogLogging<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.ClearProviders(); // у консоль пише лише Serilog (без дублів)
        var env = builder.Environment;
        var configuration = builder.Configuration;

        builder.Services.AddSerilog((services, cfg) => cfg
            .MinimumLevel.Is(env.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .ReadFrom.Configuration(configuration)   // секція Serilog в appsettings сервісу перевизначає значення за замовчуванням
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", env.ApplicationName)
            .Enrich.WithProperty("Environment", env.EnvironmentName)
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .WriteTo.Console(new CompactJsonFormatter()),
            writeToProviders: true); // записи йдуть і в провайдер OpenTelemetry -> Structured logs у Dashboard

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(o =>
        {
            o.IncludeFormattedMessage = true;
            o.IncludeScopes = true;
        });

        var isDev = builder.Environment.IsDevelopment();
        var ratio = builder.Configuration.GetValue<double?>("Telemetry:SamplingRatio") ?? 1.0;

        builder.Services.AddOpenTelemetry()
            .WithMetrics(m => m
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("Wearcast.*")
                .AddMeter("Wearcast.Wardrobe.Cache")      // Лаб. 06: метрики кешу cache.hits / cache.misses (L1/L2)
                .AddMeter("Wearcast.Feedback.Cache")
                .AddMeter("Wearcast.Aggregator.Cache"))
            .WithTracing(t =>
            {
                if (!isDev) // у Development 100 %, у Production відсоток із Telemetry:SamplingRatio
                    t.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(ratio)));

                t.AddSource(builder.Environment.ApplicationName)
                 .AddSource("Wearcast.*")          // власні spans бізнес-операцій
                 .AddSource("MongoDB.Driver")      // вбудоване трасування драйвера MongoDB 3.x (Feedback)
                 .AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources") // MongoDB DiagnosticSources (драйвер 2.x або пакет-розширення)
                 .AddAspNetCoreInstrumentation(o => o.Filter = ctx =>
                     !ctx.Request.Path.StartsWithSegments(HealthEndpointPath) &&
                     !ctx.Request.Path.StartsWithSegments(AlivenessEndpointPath))
                 .AddHttpClientInstrumentation()
                 .AddSqlClientInstrumentation();   // Dapper (Recommendations) і EF Core (Wardrobe)
            });

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            builder.Services.AddOpenTelemetry().UseOtlpExporter();

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapHealthChecks(HealthEndpointPath);
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }
        return app;
    }
}
