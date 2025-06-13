using ProductService.Server.Models;

namespace ProductService.Server.Services {
    public interface IProductBusinessService {
        Task<Product?> GetProductByIdAsync(int id);
        Task<(IEnumerable<Product> products, int totalCount)> GetProductsAsync(int page, int pageSize, string? category = null);
        Task<Product> CreateProductAsync(Product product);
        Task<Product?> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(int id);
    }
}