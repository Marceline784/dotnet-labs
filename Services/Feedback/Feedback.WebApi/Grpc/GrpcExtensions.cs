namespace Feedback.WebApi.GrpcServices;

public static class GrpcExtensions
{
    public static IServiceCollection AddFeedbackGrpc(this IServiceCollection services)
    {
        services.AddGrpc(o => o.Interceptors.Add<LoggingInterceptor>());
        return services;
    }

    public static WebApplication MapFeedbackGrpc(this WebApplication app)
    {
        app.MapGrpcService<FeedbackGrpcService>();
        return app;
    }
}
