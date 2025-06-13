namespace ProductService.Client.Configuration {
    public class GrpcClientSettings {
        public string ServerAddress { get; set; } = "https://localhost:5001";
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetryAttempts { get; set; } = 3;
        public bool EnableRetry { get; set; } = true;
        public bool ValidateCertificate { get; set; } = true;
    }
}