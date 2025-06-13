using Grpc.Core;
using ProductService.Grpc.Contracts;
using ProductService.Server.Models;

namespace ProductService.Server.Services {
    public class ProductGrpcService(IProductBusinessService productService, ILogger<ProductGrpcService> logger) : Grpc.Contracts.ProductService.ProductServiceBase {
        private readonly IProductBusinessService _productService = productService;
        private readonly ILogger<ProductGrpcService> _logger = logger;

        public override async Task<GetProductResponse> GetProduct(GetProductRequest request, ServerCallContext context) {
            try {
                _logger.LogInformation("Getting product with ID: {ProductId}", request.Id);

                var product = await _productService.GetProductByIdAsync(request.Id);

                if (product == null) {
                    return new GetProductResponse {
                        Success = false,
                        Message = $"Product with ID {request.Id} not found"
                    };
                }

                return new GetProductResponse {
                    Product = MapToProductDto(product),
                    Success = true,
                    Message = "Product retrieved successfully"
                };
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error getting product with ID: {ProductId}", request.Id);
                throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
            }
        }

        public override async Task<GetProductsResponse> GetProducts(GetProductsRequest request, ServerCallContext context) {
            try {
                _logger.LogInformation("Getting products - Page: {Page}, PageSize: {PageSize}", request.Page, request.PageSize);

                var (products, totalCount) = await _productService.GetProductsAsync(request.Page, request.PageSize, request.Category);

                var response = new GetProductsResponse {
                    TotalCount = totalCount,
                    Success = true,
                    Message = "Products retrieved successfully"
                };

                response.Products.AddRange(products.Select(MapToProductDto));

                return response;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error getting products");
                throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
            }
        }

        public override async Task<CreateProductResponse> CreateProduct(CreateProductRequest request, ServerCallContext context) {
            try {
                _logger.LogInformation("Creating product: {ProductName}", request.Name);

                var product = new Product {
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    Category = request.Category,
                    Stock = request.Stock
                };

                var createdProduct = await _productService.CreateProductAsync(product);

                return new CreateProductResponse {
                    Product = MapToProductDto(createdProduct),
                    Success = true,
                    Message = "Product created successfully"
                };
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error creating product: {ProductName}", request.Name);
                throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
            }
        }

        public override async Task<UpdateProductResponse> UpdateProduct(UpdateProductRequest request, ServerCallContext context) {
            try {
                _logger.LogInformation("Updating product with ID: {ProductId}", request.Id);

                var product = new Product {
                    Id = request.Id,
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    Category = request.Category,
                    Stock = request.Stock
                };

                var updatedProduct = await _productService.UpdateProductAsync(product);

                if (updatedProduct == null) {
                    return new UpdateProductResponse {
                        Success = false,
                        Message = $"Product with ID {request.Id} not found"
                    };
                }

                return new UpdateProductResponse {
                    Product = MapToProductDto(updatedProduct),
                    Success = true,
                    Message = "Product updated successfully"
                };
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error updating product with ID: {ProductId}", request.Id);
                throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
            }
        }

        public override async Task<DeleteProductResponse> DeleteProduct(DeleteProductRequest request, ServerCallContext context) {
            try {
                _logger.LogInformation("Deleting product with ID: {ProductId}", request.Id);

                var result = await _productService.DeleteProductAsync(request.Id);

                return new DeleteProductResponse {
                    Success = result,
                    Message = result ? "Product deleted successfully" : $"Product with ID {request.Id} not found"
                };
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error deleting product with ID: {ProductId}", request.Id);
                throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
            }
        }

        public override async Task GetProductStream(GetProductsRequest request, IServerStreamWriter<ProductDto> responseStream, ServerCallContext context) {
            try {
                _logger.LogInformation("Streaming products");

                var (products, _) = await _productService.GetProductsAsync(request.Page, request.PageSize, request.Category);

                foreach (var product in products) {
                    if (context.CancellationToken.IsCancellationRequested)
                        break;

                    await responseStream.WriteAsync(MapToProductDto(product));
                    await Task.Delay(100, context.CancellationToken); // Simulate processing time
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error streaming products");
                throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
            }
        }

        private static ProductDto MapToProductDto(Product product) {
            return new ProductDto {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Category = product.Category,
                Stock = product.Stock,
                CreatedAt = product.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedAt = product.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }
    }
}