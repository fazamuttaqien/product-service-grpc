namespace ProductService.Server.Configuration {
    public class DatabaseSettings {
        public string ConnectionString { get; set; } = string.Empty;
        public int CommandTimeout { get; set; } = 30;
        public int MaxRetryCount { get; set; } = 3;
    }
}