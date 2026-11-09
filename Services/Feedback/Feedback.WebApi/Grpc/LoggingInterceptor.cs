using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Feedback.WebApi.GrpcServices;

/// <summary>Структуроване логування вхідних gRPC-викликів: метод, peer, статус, тривалість.</summary>
public sealed class LoggingInterceptor(ILogger<LoggingInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var sw = Stopwatch.StartNew();
        var correlationId = context.RequestHeaders.GetValue("x-correlation-id");
        Activity.Current?.SetTag("rpc.correlation_id", correlationId);
        try
        {
            var response = await continuation(request, context);
            logger.LogInformation("gRPC {Method} from {Peer}: OK in {Elapsed} ms, correlation {GrpcCorrelationId}",
                context.Method, context.Peer, sw.ElapsedMilliseconds, correlationId);
            return response;
        }
        catch (RpcException ex)
        {
            logger.LogWarning(ex, "gRPC {Method} from {Peer}: {Status} in {Elapsed} ms",
                context.Method, context.Peer, ex.StatusCode, sw.ElapsedMilliseconds);
            throw;
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("gRPC {Method} from {Peer}: cancelled", context.Method, context.Peer);
            throw new RpcException(new Status(StatusCode.Cancelled, "Cancelled"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "gRPC {Method} from {Peer}: unhandled error in {Elapsed} ms",
                context.Method, context.Peer, sw.ElapsedMilliseconds);
            throw new RpcException(new Status(StatusCode.Internal, "Internal error"));
        }
    }
}
