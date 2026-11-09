namespace Wardrobe.Api.GrpcServices;

public static class GrpcExtensions
{
    public static IServiceCollection AddWardrobeGrpc(this IServiceCollection services)
    {
        services.AddGrpc(o => o.Interceptors.Add<LoggingInterceptor>());
        return services;
    }

    public static WebApplication MapWardrobeGrpc(this WebApplication app)
    {
        app.MapGrpcService<WardrobeGrpcService>();
        return app;
    }
}
