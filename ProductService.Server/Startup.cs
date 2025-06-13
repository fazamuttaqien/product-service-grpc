using ProductService.Server.Services;
using ProductService.Server.Repositories;
using ProductService.Server.Configuration;
using ProductService.Server.Interceptors;

namespace ProductService.Server {
    public class Startup(IConfiguration configuration) {
        public IConfiguration Configuration { get; } = configuration;

        public void ConfigureServices(IServiceCollection services) {
            // Configuration
            services.Configure<DatabaseSettings>(Configuration.GetSection("Database"));
            services.Configure<GrpcSettings>(Configuration.GetSection("Grpc"));

            // Repositories
            services.AddSingleton<IProductRepository, ProductRepository>();

            // Services
            services.AddScoped<IProductBusinessService, ProductBusinessService>();

            // gRPC Services
            services.AddGrpc(options => {
                options.Interceptors.Add<LoggingInterceptor>();
                options.Interceptors.Add<ErrorInterceptor>();
                options.MaxReceiveMessageSize = 4 * 1024 * 1024; // 4MB
                options.MaxSendMessageSize = 4 * 1024 * 1024; // 4MB
            });

            // Health Checks
            services.AddGrpcHealthChecks();

            // CORS (jika diperlukan untuk gRPC-Web)
            services.AddCors(options => {
                options.AddDefaultPolicy(policy => {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseCors();

            app.UseEndpoints(endpoints => {
                endpoints.MapGrpcService<ProductGrpcService>();
                endpoints.MapGrpcHealthChecksService();

                endpoints.MapGet("/", async context => {
                    await context.Response.WriteAsync("gRPC Product Service is running!");
                });
            });
        }
    }
}