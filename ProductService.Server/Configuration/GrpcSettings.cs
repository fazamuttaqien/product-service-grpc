namespace ProductService.Server.Configuration {
    public class GrpcSettings {
        public int Port { get; set; } = 5001;
        public int MaxReceiveMessageSize { get; set; } = 4 * 1024 * 1024;
        public int MaxSendMessageSize { get; set; } = 4 * 1024 * 1024;
        public bool EnableReflection { get; set; } = false;
        public bool EnableDetailedErrors { get; set; } = false;
    }
}