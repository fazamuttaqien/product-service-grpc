using Grpc.Core;
using Microsoft.Extensions.Logging;
using ProductService.Grpc.Contracts;

namespace ProductService.Client.Services {
    public class ProductClientService(
        Grpc.Contracts.ProductService.ProductServiceClient grpcClient,
        ILogger<ProductClientService> logger) : IProductClientService {
        private readonly Grpc.Contracts.ProductService.ProductServiceClient _grpcClient = grpcClient;
        private readonly ILogger<ProductClientService> _logger = logger;

        public async Task GetAllProductsAsync() {
            try {
                _logger.LogInformation("Requesting all products");

                var request = new GetProductsRequest {
                    Page = 1,
                    PageSize = 10
                };

                var response = await _grpcClient.GetProductsAsync(request);

                if (response.Success) {
                    Console.WriteLine($"Retrieved {response.Products.Count} products (Total: {response.TotalCount}):");
                    foreach (var product in response.Products) {
                        DisplayProduct(product);
                    }
                }
                else {
                    Console.WriteLine($"Failed to get products: {response.Message}");
                }
            }
            catch (RpcException ex) {
                _logger.LogError(ex, "gRPC error getting products");
                Console.WriteLine($"gRPC Error: {ex.Status.StatusCode} - {ex.Status.Detail}");
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error getting products");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public async Task GetProductByIdAsync(int id) {
            try {
                _logger.LogInformation("Requesting product with ID: {ProductId}", id);

                var request = new GetProductRequest { Id = id };
                var response = await _grpcClient.GetProductAsync(request);

                if (response.Success && response.Product != null) {
                    Console.WriteLine("Product found:");
                    DisplayProduct(response.Product);
                }
                else {
                    Console.WriteLine($"Product not found: {response.Message}");
                }
            }
            catch (RpcException ex) {
                _logger.LogError(ex, "gRPC error getting product {ProductId}", id);
                Console.WriteLine($"gRPC Error: {ex.Status.StatusCode} - {ex.Status.Detail}");
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error getting product {ProductId}", id);
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public async Task CreateProductAsync() {
            try {
                _logger.LogInformation("Creating new product");

                var request = new CreateProductRequest {
                    Name = "New Gaming Mouse",
                    Description = "High-precision gaming mouse with RGB lighting",
                    Price = 750000,
                    Category = "Electronics",
                    Stock = 20
                };

                var response = await _grpcClient.CreateProductAsync(request);

                if (response.Success && response.Product != null) {
                    Console.WriteLine("Product created successfully:");
                    DisplayProduct(response.Product);
                }
                else {
                    Console.WriteLine($"Failed to create product: {response.Message}");
                }
            }
            catch (RpcException ex) {
                _logger.LogError(ex, "gRPC error creating product");
                Console.WriteLine($"gRPC Error: {ex.Status.StatusCode} - {ex.Status.Detail}");
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error creating product");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public async Task UpdateProductAsync(int id) {
            try {
                _logger.LogInformation("Updating product with ID: {ProductId}", id);

                var request = new UpdateProductRequest {
                    Id = id,
                    Name = "Updated Gaming Laptop",
                    Description = "Updated high-performance gaming laptop with latest specs",
                    Price = 16000000,
                    Category = "Electronics",
                    Stock = 8
                };

                var response = await _grpcClient.UpdateProductAsync(request);

                if (response.Success && response.Product != null) {
                    Console.WriteLine("Product updated successfully:");
                    DisplayProduct(response.Product);
                }
                else {
                    Console.WriteLine($"Failed to update product: {response.Message}");
                }
            }
            catch (RpcException ex) {
                _logger.LogError(ex, "gRPC error updating product {ProductId}", id);
                Console.WriteLine($"gRPC Error: {ex.Status.StatusCode} - {ex.Status.Detail}");
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public async Task DeleteProductAsync(int id) {
            try {
                _logger.LogInformation("Deleting product with ID: {ProductId}", id);

                var request = new DeleteProductRequest { Id = id };
                var response = await _grpcClient.DeleteProductAsync(request);

                if (response.Success) {
                    Console.WriteLine($"Product deleted successfully: {response.Message}");
                }
                else {
                    Console.WriteLine($"Failed to delete product: {response.Message}");
                }
            }
            catch (RpcException ex) {
                _logger.LogError(ex, "gRPC error deleting product {ProductId}", id);
                Console.WriteLine($"gRPC Error: {ex.Status.StatusCode} - {ex.Status.Detail}");
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public async Task GetProductStreamAsync() {
            try {
                _logger.LogInformation("Starting product stream");

                var request = new GetProductsRequest {
                    Page = 1,
                    PageSize = 5
                };

                using var streamCall = _grpcClient.GetProductStream(request);
                var responseStream = streamCall.ResponseStream;

                Console.WriteLine("Streaming products:");
                var count = 0;

                await foreach (var product in responseStream.ReadAllAsync()) {
                    count++;
                    Console.WriteLine($"Stream #{count}:");
                    DisplayProduct(product);
                    Console.WriteLine();
                }

                Console.WriteLine($"Stream completed. Received {count} products.");
            }
            catch (RpcException ex) {
                _logger.LogError(ex, "gRPC error streaming products");
                Console.WriteLine($"gRPC Error: {ex.Status.StatusCode} - {ex.Status.Detail}");
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error streaming products");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static void DisplayProduct(ProductDto product) {
            Console.WriteLine($"  ID: {product.Id}");
            Console.WriteLine($"  Name: {product.Name}");
            Console.WriteLine($"  Description: {product.Description}");
            Console.WriteLine($"  Price: Rp {product.Price:N0}");
            Console.WriteLine($"  Category: {product.Category}");
            Console.WriteLine($"  Stock: {product.Stock}");
            Console.WriteLine($"  Created: {product.CreatedAt}");
            Console.WriteLine($"  Updated: {product.UpdatedAt}");
        }
    }
}