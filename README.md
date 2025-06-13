# gRPC Production Deployment Guide

## Struktur Namespace

```
ProductService/
├── ProductService.Server/
│   ├── Configuration/
│   │   ├── DatabaseSettings.cs
│   │   └── GrpcSettings.cs
│   ├── Interceptors/
│   │   ├── LoggingInterceptor.cs
│   │   └── ErrorInterceptor.cs
│   ├── Models/
│   │   └── Product.cs
│   ├── Repositories/
│   │   ├── IProductRepository.cs
│   │   └── ProductRepository.cs
│   ├── Services/
│   │   ├── IProductBusinessService.cs
│   │   ├── ProductBusinessService.cs
│   │   └── ProductGrpcService.cs
│   ├── Protos/
│   │   └── product.proto
│   ├── Program.cs
│   ├── Startup.cs
│   └── appsettings.json
├── ProductService.Client/
│   ├── Configuration/
│   │   └── GrpcClientSettings.cs
│   ├── Interceptors/
│   │   ├── ClientLoggingInterceptor.cs
│   │   └── ClientRetryInterceptor.cs
│   ├── Services/
│   │   ├── IProductClientService.cs
│   │   ├── ProductClientService.cs
│   │   └── ServiceCollectionExtensions.cs
│   ├── Program.cs
│   └── appsettings.json
└── ProductService.Grpc.Contracts/
    └── Generated proto files
```

## Setup & Installation

### 1. Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 atau VS Code
- Protocol Buffers Compiler (protoc)

### 2. Build Solution
```bash
# Clone or create the project structure
dotnet new sln -n ProductService

# Add projects
dotnet sln add ProductService.Server/ProductService.Server.csproj
dotnet sln add ProductService.Client/ProductService.Client.csproj
dotnet sln add ProductService.Contracts/ProductService.Contracts.csproj

# Add reference
dotnet add ProductService.Server reference ProductService.Contracts
dotnet add ProductService.Client reference ProductService.Contracts

# Restore packages
dotnet restore

# Build solution
dotnet build
```

### 3. Running the Application

#### Server
```bash
cd ProductService.Server
dotnet run
```

#### Client
```bash
cd ProductService.Client
dotnet run
```

## Production Configuration

### 1. Server Configuration (appsettings.Production.json)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "ProductService": "Information"
    }
  },
  "Database": {
    "ConnectionString": "Server=prod-db-server;Database=ProductDB;User Id=app_user;Password=secure_password;Encrypt=true;",
    "CommandTimeout": 60,
    "MaxRetryCount": 5
  },
  "Grpc": {
    "Port": 443,
    "MaxReceiveMessageSize": 8388608,
    "MaxSendMessageSize": 8388608,
    "EnableReflection": false,
    "EnableDetailedErrors": false
  }
}
```

### 2. Security Considerations

#### TLS Configuration
```csharp
// In Program.cs
webBuilder.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Any, 443, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
        listenOptions.UseHttps("certificate.pfx", "certificate_password");
    });
});
```

#### Authentication & Authorization
```csharp
// Add to Startup.cs
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Configuration["Jwt:Issuer"],
            ValidAudience = Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]))
        };
    });

services.AddAuthorization();

// In Configure method
app.UseAuthentication();
app.UseAuthorization();
```

### 3. Health Checks
```csharp
// Server health check endpoint
services.AddGrpcHealthChecks()
    .AddCheck("database", () => HealthCheckResult.Healthy())
    .AddCheck("external_service", () => HealthCheckResult.Healthy());
```

### 4. Load Balancing
```csharp
// Client-side load balancing
services.AddGrpcClient<ProductService.ProductServiceClient>(options =>
{
    options.Address = new Uri("dns:///product-service");
})
.ConfigureChannel(options =>
{
    options.ServiceConfig = new ServiceConfig
    {
        LoadBalancingConfigs = { new RoundRobinConfig() }
    };
});
```

## Docker Deployment

### 1. Server Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ProductService.Server/ProductService.Server.csproj", "ProductService.Server/"]
RUN dotnet restore "ProductService.Server/ProductService.Server.csproj"
COPY . .
WORKDIR "/src/ProductService.Server"
RUN dotnet build "ProductService.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProductService.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductService.Server.dll"]
```

### 2. Client Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ProductService.Client/ProductService.Client.csproj", "ProductService.Client/"]
RUN dotnet restore "ProductService.Client/ProductService.Client.csproj"
COPY . .
WORKDIR "/src/ProductService.Client"
RUN dotnet build "ProductService.Client.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProductService.Client.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductService.Client.dll"]
```

### 3. Docker Compose
```yaml
version: '3.8'

services:
  product-service-server:
    build:
      context: .
      dockerfile: ProductService.Server/Dockerfile
    ports:
      - "5001:80"
      - "5002:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=https://+:443;http://+:80
      - Database__ConnectionString=Server=db;Database=ProductDB;User Id=sa;Password=YourStrong@Passw0rd;
    depends_on:
      - db
    networks:
      - product-network
    volumes:
      - ./certificates:/https:ro

  product-service-client:
    build:
      context: .
      dockerfile: ProductService.Client/Dockerfile
    ports:
      - "5003:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - GrpcClient__ServerUrl=https://product-service-server:443
    depends_on:
      - product-service-server
    networks:
      - product-network

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Passw0rd
    ports:
      - "1433:1433"
    volumes:
      - sql_data:/var/opt/mssql
    networks:
      - product-network

volumes:
  sql_data:

networks:
  product-network:
    driver: bridge
```

## Kubernetes Deployment

### 1. Server Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: product-service-server
  labels:
    app: product-service-server
spec:
  replicas: 3
  selector:
    matchLabels:
      app: product-service-server
  template:
    metadata:
      labels:
        app: product-service-server
    spec:
      containers:
      - name: product-service-server
        image: product-service-server:latest
        ports:
        - containerPort: 80
        - containerPort: 443
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: Database__ConnectionString
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: connection-string
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: product-service-server-service
spec:
  selector:
    app: product-service-server
  ports:
  - name: http
    port: 80
    targetPort: 80
  - name: https
    port: 443
    targetPort: 443
  type: ClusterIP
```

### 2. Client Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: product-service-client
  labels:
    app: product-service-client
spec:
  replicas: 2
  selector:
    matchLabels:
      app: product-service-client
  template:
    metadata:
      labels:
        app: product-service-client
    spec:
      containers:
      - name: product-service-client
        image: product-service-client:latest
        ports:
        - containerPort: 80
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: GrpcClient__ServerUrl
          value: "https://product-service-server-service:443"
        resources:
          requests:
            memory: "128Mi"
            cpu: "100m"
          limits:
            memory: "256Mi"
            cpu: "200m"
---
apiVersion: v1
kind: Service
metadata:
  name: product-service-client-service
spec:
  selector:
    app: product-service-client
  ports:
  - port: 80
    targetPort: 80
  type: LoadBalancer
```

### 3. Ingress Configuration
```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: product-service-ingress
  annotations:
    kubernetes.io/ingress.class: nginx
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
    nginx.ingress.kubernetes.io/backend-protocol: "GRPC"
spec:
  tls:
  - hosts:
    - api.yourcompany.com
    secretName: tls-secret
  rules:
  - host: api.yourcompany.com
    http:
      paths:
      - path: /ProductService
        pathType: Prefix
        backend:
          service:
            name: product-service-server-service
            port:
              number: 443
```

## Monitoring & Observability

### 1. Logging Configuration
```csharp
// Add structured logging
services.AddLogging(builder =>
{
    builder.AddConsole()
           .AddApplicationInsights()
           .AddSerilog();
});

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.ApplicationInsights(TelemetryConfiguration.CreateDefault(), TelemetryConverter.Traces)
    .CreateLogger();
```

### 2. Metrics Collection
```csharp
// Add metrics
services.AddSingleton<IMetrics, MetricsLogger>();

// Custom metrics in service
public class ProductGrpcService : ProductService.ProductServiceBase
{
    private readonly IMetrics _metrics;
    
    public override async Task<GetProductResponse> GetProduct(GetProductRequest request, ServerCallContext context)
    {
        using var activity = _metrics.StartActivity("GetProduct");
        _metrics.Counter("grpc_requests_total")
            .WithTag("method", "GetProduct")
            .Increment();
            
        // Service implementation
    }
}
```

### 3. Distributed Tracing
```csharp
// Add OpenTelemetry
services.AddOpenTelemetry()
    .WithTracing(builder =>
    {
        builder.SetSampler(new TraceIdRatioBasedSampler(0.1))
               .AddSource("ProductService")
               .AddGrpcClientInstrumentation()
               .AddGrpcCoreInstrumentation()
               .AddJaegerExporter();
    });
```

## Performance Optimization

### 1. Connection Pooling
```csharp
// Server-side connection pooling
services.Configure<GrpcServiceOptions>(options =>
{
    options.MaxReceiveMessageSize = 8 * 1024 * 1024; // 8MB
    options.MaxSendMessageSize = 8 * 1024 * 1024;    // 8MB
    options.EnableDetailedErrors = false;
    options.CompressionProviders.Add(new GzipCompressionProvider(CompressionLevel.Optimal));
});

// Client-side connection pooling
services.AddGrpcClient<ProductService.ProductServiceClient>(options =>
{
    options.Address = new Uri("https://localhost:5001");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    return new SocketsHttpHandler()
    {
        PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
        KeepAlivePingDelay = TimeSpan.FromSeconds(60),
        KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
        EnableMultipleHttp2Connections = true
    };
});
```

### 2. Caching Strategy
```csharp
// Add Redis caching
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = Configuration.GetConnectionString("Redis");
    options.InstanceName = "ProductService";
});

// Implement caching in service
public class ProductBusinessService : IProductBusinessService
{
    private readonly IDistributedCache _cache;
    
    public async Task<Product> GetProductAsync(int id)
    {
        var cacheKey = $"product:{id}";
        var cachedProduct = await _cache.GetStringAsync(cacheKey);
        
        if (cachedProduct != null)
        {
            return JsonSerializer.Deserialize<Product>(cachedProduct);
        }
        
        var product = await _repository.GetProductAsync(id);
        
        if (product != null)
        {
            var serializedProduct = JsonSerializer.Serialize(product);
            await _cache.SetStringAsync(cacheKey, serializedProduct, 
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                });
        }
        
        return product;
    }
}
```

## Security Best Practices

### 1. API Key Authentication
```csharp
// Custom interceptor for API key validation
public class ApiKeyInterceptor : Interceptor
{
    private readonly IConfiguration _configuration;
    
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var apiKey = context.RequestHeaders.GetValue("x-api-key");
        
        if (string.IsNullOrEmpty(apiKey) || !IsValidApiKey(apiKey))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid API key"));
        }
        
        return await continuation(request, context);
    }
    
    private bool IsValidApiKey(string apiKey)
    {
        var validApiKeys = _configuration.GetSection("ApiKeys").Get<string[]>();
        return validApiKeys?.Contains(apiKey) == true;
    }
}
```

### 2. Rate Limiting
```csharp
// Add rate limiting
services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("GrpcPolicy", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });
});

// Apply rate limiting
[EnableRateLimiting("GrpcPolicy")]
public class ProductGrpcService : ProductService.ProductServiceBase
{
    // Service methods
}
```

## Troubleshooting Guide

### Common Issues

1. **Connection Refused**
   - Check if server is running on correct port
   - Verify firewall settings
   - Ensure proper TLS configuration

2. **Deadline Exceeded**
   - Increase client timeout settings
   - Check database connection performance
   - Verify network latency

3. **SSL/TLS Errors**
   - Validate certificate configuration
   - Check certificate expiration
   - Verify trust chain

### Debug Commands
```bash
# Check gRPC server health
grpcurl -plaintext localhost:5000 grpc.health.v1.Health/Check

# List available services
grpcurl -plaintext localhost:5000 list

# Test specific method
grpcurl -plaintext -d '{"id": 1}' localhost:5000 ProductService/GetProduct

# Monitor Docker logs
docker logs -f product-service-server

# Check Kubernetes pods
kubectl get pods -l app=product-service-server
kubectl logs -f deployment/product-service-server
```

## Deployment Checklist

### Pre-Production
- [ ] Load testing completed
- [ ] Security audit passed
- [ ] Database migration scripts tested
- [ ] Monitoring and alerting configured
- [ ] Backup and recovery procedures documented
- [ ] SSL certificates installed and validated

### Production Deployment
- [ ] Environment variables configured
- [ ] Database connections tested
- [ ] Health checks responding
- [ ] Load balancer configured
- [ ] Monitoring dashboards active
- [ ] Log aggregation working
- [ ] Performance metrics collected

### Post-Deployment
- [ ] Smoke tests passed
- [ ] Performance benchmarks met
- [ ] Error rates within acceptable limits
- [ ] Team notified of deployment
- [ ] Documentation updated
- [ ] Rollback plan verified

## Support & Maintenance

### Regular Tasks
- Monitor application performance metrics
- Review and rotate SSL certificates
- Update dependencies and security patches
- Backup database and configuration files
- Review and optimize database queries
- Clean up old log files and metrics data

### Contact Information
- **Development Team**: dev-team@company.com
- **DevOps Team**: devops@company.com  
- **On-Call Support**: +1-555-ON-CALL

### Resources
- [gRPC Documentation](https://grpc.io/docs/)
- [.NET gRPC Guide](https://docs.microsoft.com/en-us/aspnet/core/grpc/)
- [Protocol Buffers Guide](https://developers.google.com/protocol-buffers/)
- [Docker Documentation](https://docs.docker.com/)
- [Kubernetes Documentation](https://kubernetes.io/docs/)
