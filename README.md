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
RUN dotnet build "Product