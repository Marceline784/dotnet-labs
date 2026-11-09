namespace Recommendations.Api.GrpcServices;

public static class GrpcExtensions
{
    public static IServiceCollection AddRecommendationsGrpc(this IServiceCollection services)
    {
        services.AddGrpc(o => o.Interceptors.Add<LoggingInterceptor>());
        return services;
    }

    public static WebApplication MapRecommendationsGrpc(this WebApplication app)
    {
        app.MapGrpcService<RecommendationsGrpcService>();
        return app;
    }
}
