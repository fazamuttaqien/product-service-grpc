using ProductService.Server.Models;
using ProductService.Server.Repositories;

namespace ProductService.Server.Services {
    public class ProductBusinessService(IProductRepository repository, ILogger<ProductBusinessService> logger) : IProductBusinessService {
        private readonly IProductRepository _repository = repository;
        private readonly ILogger<ProductBusinessService> _logger = logger;

        public async Task<Product?> GetProductByIdAsync(int id) {
            if (id <= 0)
                throw new ArgumentException("Product ID must be greater than 0", nameof(id));

            return await _repository.GetByIdAsync(id);
        }

        public async Task<(IEnumerable<Product> products, int totalCount)> GetProductsAsync(int page, int pageSize, string? category = null) {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Limit maximum page size

            return await _repository.GetAllAsync(page, pageSize, category);
        }

        public async Task<Product> CreateProductAsync(Product product) {
            ValidateProduct(product);

            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;

            return await _repository.CreateAsync(product);
        }

        public async Task<Product?> UpdateProductAsync(Product product) {
            ValidateProduct(product);

            var existingProduct = await _repository.GetByIdAsync(product.Id);
            if (existingProduct == null)
                return null;

            product.CreatedAt = existingProduct.CreatedAt;
            product.UpdatedAt = DateTime.UtcNow;

            return await _repository.UpdateAsync(product);
        }

        public async Task<bool> DeleteProductAsync(int id) {
            if (id <= 0)
                throw new ArgumentException("Product ID must be greater than 0", nameof(id));

            return await _repository.DeleteAsync(id);
        }

        private static void ValidateProduct(Product product) {
            if (string.IsNullOrWhiteSpace(product.Name))
                throw new ArgumentException("Product name is required", nameof(product.Name));

            if (product.Price < 0)
                throw new ArgumentException("Product price cannot be negative", nameof(product.Price));

            if (product.Stock < 0)
                throw new ArgumentException("Product stock cannot be negative", nameof(product.Stock));
        }
    }
}