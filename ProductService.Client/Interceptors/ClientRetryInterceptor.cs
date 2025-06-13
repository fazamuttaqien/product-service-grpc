using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductService.Client.Configuration;

namespace ProductService.Client.Interceptors {
    public class ClientRetryInterceptor(ILogger<ClientRetryInterceptor> logger, IOptions<GrpcClientSettings> settings) : Interceptor {
        private readonly ILogger<ClientRetryInterceptor> _logger = logger;
        private readonly GrpcClientSettings _settings = settings.Value;

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation) {
            var call = continuation(request, context);

            return new AsyncUnaryCall<TResponse>(
                RetryCall(call.ResponseAsync, request, context, continuation),
                call.ResponseHeadersAsync,
                call.GetStatus,
                call.GetTrailers,
                call.Dispose);
        }

        private async Task<TResponse> RetryCall<TRequest, TResponse>(
            Task<TResponse> responseTask,
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
            where TRequest : class
            where TResponse : class {
            var attempt = 1;
            var maxAttempts = _settings.MaxRetryAttempts;

            while (attempt <= maxAttempts) {
                try {
                    return await responseTask;
                }
                catch (RpcException ex) when (ShouldRetry(ex.StatusCode) && attempt < maxAttempts) {
                    _logger.LogWarning("gRPC call failed on attempt {Attempt}/{MaxAttempts}. Status: {Status}. Retrying...",
                        attempt, maxAttempts, ex.StatusCode);

                    attempt++;
                    var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt - 1) * 1000); // Exponential backoff
                    await Task.Delay(delay);

                    var retryCall = continuation(request, context);
                    responseTask = retryCall.ResponseAsync;
                }
            }

            // This should not be reached, but just in case
            return await responseTask;
        }

        private static bool ShouldRetry(StatusCode statusCode) {
            return statusCode == StatusCode.Unavailable ||
                   statusCode == StatusCode.DeadlineExceeded ||
                   statusCode == StatusCode.Internal;
        }
    }
}