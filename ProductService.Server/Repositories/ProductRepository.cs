using ProductService.Server.Models;
using System.Collections.Concurrent;

namespace ProductService.Server.Repositories {
    public class ProductRepository : IProductRepository {
        private readonly ConcurrentDictionary<int, Product> _products;
        private int _nextId = 1;

        public ProductRepository() {
            _products = new ConcurrentDictionary<int, Product>();
            SeedData();
        }

        public Task<Product?> GetByIdAsync(int id) {
            _products.TryGetValue(id, out var product);
            return Task.FromResult(product);
        }

        public Task<(IEnumerable<Product> products, int totalCount)> GetAllAsync(int page, int pageSize, string? category = null) {
            var query = _products.Values.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(category)) {
                query = query.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            var totalCount = query.Count();
            var products = query
                .OrderBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Task.FromResult((products.AsEnumerable(), totalCount));
        }

        public Task<Product> CreateAsync(Product product) {
            product.Id = Interlocked.Increment(ref _nextId);
            _products.TryAdd(product.Id, product);
            return Task.FromResult(product);
        }

        public Task<Product> UpdateAsync(Product product) {
            _products.TryUpdate(product.Id, product, _products[product.Id]);
            return Task.FromResult(product);
        }

        public Task<bool> DeleteAsync(int id) {
            return Task.FromResult(_products.TryRemove(id, out _));
        }

        private void SeedData() {
            var sampleProducts = new[]
            {
                new Product { Name = "Laptop Gaming", Description = "High-performance gaming laptop", Price = 15000000, Category = "Electronics", Stock = 10, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Product { Name = "Smartphone", Description = "Latest Android smartphone", Price = 8000000, Category = "Electronics", Stock = 25, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Product { Name = "Office Chair", Description = "Ergonomic office chair", Price = 2500000, Category = "Furniture", Stock = 15, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Product { Name = "Coffee Maker", Description = "Automatic coffee maker", Price = 1200000, Category = "Appliances", Stock = 8, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Product { Name = "Running Shoes", Description = "Professional running shoes", Price = 1500000, Category = "Sports", Stock = 30, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            };

            foreach (var product in sampleProducts) {
                product.Id = Interlocked.Increment(ref _nextId);
                _products.TryAdd(product.Id, product);
            }
        }
    }
}