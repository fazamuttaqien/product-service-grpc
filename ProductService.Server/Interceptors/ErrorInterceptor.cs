using Grpc.Core;
using Grpc.Core.Interceptors;

namespace ProductService.Server.Interceptors {
    public class ErrorInterceptor(ILogger<ErrorInterceptor> logger) : Interceptor {
        private readonly ILogger<ErrorInterceptor> _logger = logger;

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation) {
            try {
                return await continuation(request, context);
            }
            catch (ArgumentException ex) {
                _logger.LogWarning(ex, "Invalid argument in gRPC call: {Method}", context.Method);
                throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
            }
            catch (UnauthorizedAccessException ex) {
                _logger.LogWarning(ex, "Unauthorized access in gRPC call: {Method}", context.Method);
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Authentication required"));
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Unhandled exception in gRPC call: {Method}", context.Method);
                throw new RpcException(new Status(StatusCode.Internal, "An internal error occurred"));
            }
        }
    }
}