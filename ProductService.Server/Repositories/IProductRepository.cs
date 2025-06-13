using ProductService.Server.Models;

namespace ProductService.Server.Repositories {
    public interface IProductRepository {
        Task<Product?> GetByIdAsync(int id);
        Task<(IEnumerable<Product> products, int totalCount)> GetAllAsync(int page, int pageSize, string? category = null);
        Task<Product> CreateAsync(Product product);
        Task<Product> UpdateAsync(Product product);
        Task<bool> DeleteAsync(int id);
    }
}