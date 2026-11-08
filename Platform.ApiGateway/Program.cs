using System.Diagnostics;
using System.Diagnostics.Metrics;
using Platform.ServiceDefaults;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Model;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();   // Serilog, OpenTelemetry, CorrelationId (middleware стоїть ДО YARP), service discovery

// ---- YARP: маршрути й кластери з appsettings.json; адреси http://recommendations-api тощо підставляє service discovery ----
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

// ---- Метрики шлюзу (з'являються у вкладці Metrics Dashboard): запити, тривалість, частка помилок за статусом ----
var meter = new Meter("Wearcast.Gateway");
var requests = meter.CreateCounter<long>("gateway.requests");
var duration = meter.CreateHistogram<double>("gateway.request.duration", "ms");

var app = builder.Build();

// CorrelationId: береться з запиту або генерується тут (Gateway — перша точка входу); ДО MapReverseProxy.
// ServiceDefaults додає цей самий middleware автоматично, повторне підключення безпечне (ідемпотентне).
app.UseMiddleware<CorrelationIdMiddleware>();

// Лог рішення про маршрутизацію: маршрут, кластер, адреса, статус, тривалість, розмір, помилки проксіювання
app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();
    try
    {
        await next();
    }
    finally
    {
        var feature = context.Features.Get<IReverseProxyFeature>();
        var route = feature?.Route.Config.RouteId ?? "none";
        var cluster = feature?.Cluster.Config.ClusterId;
        var destination = feature?.ProxiedDestination?.Model.Config.Address;
        var status = context.Response.StatusCode;

        app.Logger.LogInformation(
            "{Method} {Path} from {ClientIp} -> route {Route}, cluster {Cluster}, destination {Destination}: {StatusCode} in {Elapsed} ms, {Size} bytes",
            context.Request.Method, context.Request.Path.Value, context.Connection.RemoteIpAddress?.ToString(),
            route, cluster, destination, status, sw.ElapsedMilliseconds, context.Response.ContentLength);

        var error = context.Features.Get<IForwarderErrorFeature>();
        if (error is not null)
            app.Logger.LogError(error.Exception, "Upstream error {ForwarderError} on route {Route}, cluster {Cluster}",
                error.Error, route, cluster);

        var tags = new KeyValuePair<string, object?>[] { new("route", route), new("status", status) };
        requests.Add(1, tags);
        duration.Record(sw.Elapsed.TotalMilliseconds, tags);
    }
});

app.MapReverseProxy();
app.MapDefaultEndpoints();
app.Run();
