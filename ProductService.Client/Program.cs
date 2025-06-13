using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProductService.Client.Services;
using ProductService.Client.Configuration;
using Serilog;

namespace ProductService.Client {
    public class Program {
        public static async Task Main(string[] args) {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs/client-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try {
                Log.Information("Starting gRPC client");

                var host = CreateHostBuilder(args).Build();

                using var scope = host.Services.CreateScope();
                var clientService = scope.ServiceProvider.GetRequiredService<IProductClientService>();

                await RunClientOperations(clientService);
            }
            catch (Exception ex) {
                Log.Fatal(ex, "Client terminated unexpectedly");
            }
            finally {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((context, services) => {
                    // Configuration
                    services.Configure<GrpcClientSettings>(context.Configuration.GetSection("GrpcClient"));

                    // gRPC Client Services
                    services.AddGrpcClientServices();
                    services.AddScoped<IProductClientService, ProductClientService>();
                });

        private static async Task RunClientOperations(IProductClientService clientService) {
            try {
                Console.WriteLine("=== gRPC Client Demo ===\n");

                // Test Get All Products
                Console.WriteLine("1. Getting all products...");
                await clientService.GetAllProductsAsync();

                Console.WriteLine("\n" + new string('-', 50) + "\n");

                // Test Get Single Product
                Console.WriteLine("2. Getting product by ID...");
                await clientService.GetProductByIdAsync(1);

                Console.WriteLine("\n" + new string('-', 50) + "\n");

                // Test Create Product
                Console.WriteLine("3. Creating new product...");
                await clientService.CreateProductAsync();

                Console.WriteLine("\n" + new string('-', 50) + "\n");

                // Test Update Product
                Console.WriteLine("4. Updating product...");
                await clientService.UpdateProductAsync(1);

                Console.WriteLine("\n" + new string('-', 50) + "\n");

                // Test Product Stream
                Console.WriteLine("5. Streaming products...");
                await clientService.GetProductStreamAsync();

                Console.WriteLine("\n" + new string('-', 50) + "\n");

                // Test Delete Product (uncomment if needed)
                // Console.WriteLine("6. Deleting product...");
                // await clientService.DeleteProductAsync(1);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error during client operations: {ex.Message}");
                Log.Error(ex, "Error during client operations");
            }
        }
    }
}