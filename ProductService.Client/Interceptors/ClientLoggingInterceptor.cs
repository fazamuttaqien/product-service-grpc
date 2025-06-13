using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace ProductService.Client.Interceptors {
    public class ClientLoggingInterceptor(ILogger<ClientLoggingInterceptor> logger) : Interceptor {
        private readonly ILogger<ClientLoggingInterceptor> _logger = logger;

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation) {
            _logger.LogInformation("Starting gRPC call: {Method}", context.Method.FullName);

            var call = continuation(request, context);

            return new AsyncUnaryCall<TResponse>(
                HandleResponse(call.ResponseAsync, context.Method.FullName),
                call.ResponseHeadersAsync,
                call.GetStatus,
                call.GetTrailers,
                call.Dispose);
        }

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation) {
            _logger.LogInformation("Starting gRPC streaming call: {Method}", context.Method.FullName);
            return continuation(request, context);
        }

        private async Task<TResponse> HandleResponse<TResponse>(Task<TResponse> responseTask, string methodName) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try {
                var response = await responseTask;
                stopwatch.Stop();

                _logger.LogInformation("Completed gRPC call: {Method} in {ElapsedMs}ms",
                    methodName, stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex) {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in gRPC call: {Method} after {ElapsedMs}ms",
                    methodName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}