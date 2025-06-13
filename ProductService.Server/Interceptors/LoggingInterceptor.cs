using Grpc.Core;
using Grpc.Core.Interceptors;

namespace ProductService.Server.Interceptors {
    public class LoggingInterceptor(ILogger<LoggingInterceptor> logger) : Interceptor {
        private readonly ILogger<LoggingInterceptor> _logger = logger;

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _logger.LogInformation("Starting gRPC call: {Method}", context.Method);

            try {
                var response = await continuation(request, context);

                stopwatch.Stop();
                _logger.LogInformation("Completed gRPC call: {Method} in {ElapsedMs}ms",
                    context.Method, stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex) {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in gRPC call: {Method} after {ElapsedMs}ms",
                    context.Method, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
            TRequest request,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            ServerStreamingServerMethod<TRequest, TResponse> continuation) {
            _logger.LogInformation("Starting gRPC streaming call: {Method}", context.Method);

            try {
                await continuation(request, responseStream, context);
                _logger.LogInformation("Completed gRPC streaming call: {Method}", context.Method);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error in gRPC streaming call: {Method}", context.Method);
                throw;
            }
        }
    }
}