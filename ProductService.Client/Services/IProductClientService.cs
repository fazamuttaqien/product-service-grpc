namespace ProductService.Client.Services {
    public interface IProductClientService {
        Task GetAllProductsAsync();
        Task GetProductByIdAsync(int id);
        Task CreateProductAsync();
        Task UpdateProductAsync(int id);
        Task DeleteProductAsync(int id);
        Task GetProductStreamAsync();
    }
}