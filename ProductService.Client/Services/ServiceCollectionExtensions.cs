using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProductService.Client.Configuration;
using ProductService.Client.Interceptors;

namespace ProductService.Client.Services {
    public static class ServiceCollectionExtensions {
        public static IServiceCollection AddGrpcClientServices(this IServiceCollection services) {
            services.AddHttpClient();

            services.AddSingleton<ClientLoggingInterceptor>();
            services.AddSingleton<ClientRetryInterceptor>();

            services.AddSingleton(provider => {
                var settings = provider.GetRequiredService<IOptions<GrpcClientSettings>>().Value;
                var loggingInterceptor = provider.GetRequiredService<ClientLoggingInterceptor>();
                var retryInterceptor = provider.GetRequiredService<ClientRetryInterceptor>();

                var httpHandler = new HttpClientHandler();
                if (!settings.ValidateCertificate) {
                    httpHandler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }

                var channel = GrpcChannel.ForAddress(settings.ServerAddress, new GrpcChannelOptions {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = 4 * 1024 * 1024,
                    MaxSendMessageSize = 4 * 1024 * 1024
                });

                var invoker = channel.Intercept(loggingInterceptor);
                if (settings.EnableRetry) {
                    invoker = invoker.Intercept(retryInterceptor);
                }

                return new Grpc.Contracts.ProductService.ProductServiceClient(invoker);
            });

            return services;
        }
    }
}