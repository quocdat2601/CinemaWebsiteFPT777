# 🚀 Deployment Guide

## 📋 Table of Contents

- [Prerequisites](#prerequisites)
- [Environment Setup](#environment-setup)
- [Database Deployment](#database-deployment)
- [Application Deployment](#application-deployment)
- [Configuration](#configuration)
- [SSL/HTTPS Setup](#sslhttps-setup)
- [Monitoring & Logging](#monitoring--logging)
- [Troubleshooting](#troubleshooting)
- [Production Checklist](#production-checklist)

## 🛠️ Prerequisites

### **System Requirements**

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| **Operating System** | Windows Server 2019 | Windows Server 2022 |
| **CPU** | 2 cores | 4+ cores |
| **RAM** | 4 GB | 8+ GB |
| **Storage** | 50 GB | 100+ GB SSD |
| **.NET Runtime** | .NET 8.0 | .NET 8.0 LTS |

### **Software Dependencies**

- **.NET 8.0 Runtime** or SDK
- **SQL Server 2019** or later
- **IIS 10** or later
- **URL Rewrite Module** for IIS
- **Application Request Routing** (optional)

### **Network Requirements**

- **Static IP address** for the server
- **Domain name** (optional but recommended)
- **Firewall access** for ports 80, 443, 1433 (SQL)
- **SSL certificate** for HTTPS

## 🌍 Environment Setup

### **Development Environment**

```bash
# Clone the repository
git clone https://github.com/quocdat2601/CinemaWebsiteFPT777.git
cd CinemaWebsiteFPT777

# Install .NET 8.0 SDK
# Download from: https://dotnet.microsoft.com/download/dotnet/8.0

# Verify installation
dotnet --version
# Should output: 8.0.x
```

### **Production Environment**

```bash
# Install .NET 8.0 Runtime on production server
# Download from: https://dotnet.microsoft.com/download/dotnet/8.0

# Install IIS with required features
# Enable: Web Server (IIS), .NET Extensibility, ASP.NET 4.8

# Install URL Rewrite Module
# Download from: https://www.iis.net/downloads/microsoft/url-rewrite
```

## 🗄️ Database Deployment

### **1. SQL Server Installation**

```bash
# Install SQL Server 2019 or later
# Choose "Database Engine Services" and "SQL Server Replication"

# Configure SQL Server Authentication Mode: Mixed Mode
# Set SA password and remember it
```

### **2. Database Creation**

```bash
# Connect to SQL Server using SQL Server Management Studio (SSMS)
# Or use sqlcmd command line tool

# Restore the database
sqlcmd -S (local) -i Cinama.sql

# Verify database creation
sqlcmd -S (local) -Q "SELECT name FROM sys.databases WHERE name = 'MovieTheater'"
```

### **3. Database Configuration**

```sql
-- Create application user with minimal permissions
USE [MovieTheater]
GO

CREATE LOGIN [MovieTheaterApp] WITH PASSWORD = 'StrongPassword123!'
GO

CREATE USER [MovieTheaterApp] FOR LOGIN [MovieTheaterApp]
GO

-- Grant necessary permissions
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO [MovieTheaterApp]
GO

-- Grant execute permissions on stored procedures if any
GRANT EXECUTE TO [MovieTheaterApp]
GO
```

### **4. Connection String**

Update `appsettings.json` with production database connection:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=MovieTheater;User Id=MovieTheaterApp;Password=StrongPassword123!;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

## 🚀 Application Deployment

### **Method 1: IIS Deployment (Recommended)**

#### **1. Publish Application**

```bash
# Publish for production
dotnet publish -c Release -o ./publish

# Or publish to specific folder
dotnet publish -c Release -o C:\inetpub\wwwroot\CinemaWebsite
```

#### **2. IIS Configuration**

```xml
<!-- web.config in publish folder -->
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\MovieTheater.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
```

#### **3. IIS Site Setup**

1. **Open IIS Manager**
2. **Create new Application Pool**:
   - .NET CLR Version: "No Managed Code"
   - Managed Pipeline Mode: "Integrated"
3. **Create new Website**:
   - Site name: "CinemaWebsite"
   - Physical path: Path to published application
   - Port: 80 (or your preferred port)
4. **Assign Application Pool** to the website

### **Method 2: Self-Hosted Deployment**

#### **1. Create Windows Service**

```bash
# Install Microsoft.Extensions.Hosting.WindowsServices
dotnet add package Microsoft.Extensions.Hosting.WindowsServices

# Create service configuration
sc create "CinemaWebsite" binPath="C:\CinemaWebsite\MovieTheater.exe"
sc description "CinemaWebsite" "Cinema Management System"
sc start "CinemaWebsite"
```

#### **2. Service Configuration**

```csharp
// Program.cs modifications
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .UseWindowsService()
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        });
```

### **Method 3: Docker Deployment**

#### **1. Dockerfile**

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MovieTheater.csproj", "./"]
RUN dotnet restore "MovieTheater.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "MovieTheater.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MovieTheater.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MovieTheater.dll"]
```

#### **2. Docker Compose**

```yaml
version: '3.8'
services:
  cinemawebsite:
    build: .
    ports:
      - "80:80"
      - "443:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=db;Database=MovieTheater;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True
    depends_on:
      - db
  
  db:
    image: mcr.microsoft.com/mssql/server:2019-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourPassword123!
    ports:
      - "1433:1433"
```

## ⚙️ Configuration

### **Environment Variables**

```bash
# Production environment variables
set ASPNETCORE_ENVIRONMENT=Production
set ASPNETCORE_URLS=http://localhost:5000;https://localhost:5001
set ConnectionStrings__DefaultConnection="Server=YOUR_SERVER;Database=MovieTheater;User Id=MovieTheaterApp;Password=YourPassword;TrustServerCertificate=True"
```

### **appsettings.Production.json**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "Logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ]
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=MovieTheater;User Id=MovieTheaterApp;Password=YourPassword;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### **Authentication Configuration**

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "your_production_google_client_id",
      "ClientSecret": "your_production_google_client_secret"
    }
  },
  "JwtSettings": {
    "SecretKey": "your_production_secret_key_at_least_32_characters_long",
    "Issuer": "https://yourdomain.com",
    "Audience": "https://yourdomain.com",
    "ExpirationInMinutes": 60
  }
}
```

## 🔒 SSL/HTTPS Setup

### **1. SSL Certificate**

#### **Let's Encrypt (Free)**

```bash
# Install Certbot for Windows
# Download from: https://certbot.eff.org/

# Generate certificate
certbot certonly --webroot -w C:\inetpub\wwwroot\CinemaWebsite -d yourdomain.com

# Auto-renewal
certbot renew --quiet
```

#### **Commercial Certificate**

1. **Purchase SSL certificate** from trusted CA
2. **Generate CSR** (Certificate Signing Request)
3. **Install certificate** in Windows Certificate Store
4. **Bind to IIS site** with HTTPS

### **2. HTTPS Configuration**

```xml
<!-- web.config HTTPS redirect -->
<system.webServer>
  <rewrite>
    <rules>
      <rule name="HTTP to HTTPS redirect" stopProcessing="true">
        <match url="(.*)" />
        <conditions>
          <add input="{HTTPS}" pattern="off" ignoreCase="true" />
        </conditions>
        <action type="Redirect" redirectType="Permanent" url="https://{HTTP_HOST}/{R:1}" />
      </rule>
    </rules>
  </rewrite>
</system.webServer>
```

## 📊 Monitoring & Logging

### **1. Application Logging**

```csharp
// Program.cs logging configuration
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq("http://localhost:5341") // Optional: Structured logging
);
```

### **2. Performance Monitoring**

```csharp
// Add performance counters
services.AddHealthChecks()
    .AddSqlServer(Configuration.GetConnectionString("DefaultConnection"))
    .AddCheck("self", () => HealthCheckResult.Healthy());

// Health check endpoint
app.MapHealthChecks("/health");
```

### **3. Error Tracking**

```csharp
// Global exception handling
app.UseExceptionHandler("/Error");
app.UseStatusCodePagesWithReExecute("/Error/{0}");

// Custom error page
app.MapControllerRoute(
    name: "Error",
    pattern: "Error/{statusCode}",
    defaults: new { controller = "Home", action = "Error" });
```

## 🔧 Troubleshooting

### **Common Issues**

#### **1. Database Connection**

```bash
# Test database connectivity
sqlcmd -S YOUR_SERVER -U MovieTheaterApp -P YourPassword -Q "SELECT 1"

# Check firewall settings
netsh advfirewall firewall add rule name="SQL Server" dir=in action=allow protocol=TCP localport=1433
```

#### **2. IIS Issues**

```bash
# Check IIS status
iisreset /status

# Verify application pool
appcmd list apppool

# Check application pool identity
appcmd list apppool "CinemaWebsite" /text:*
```

#### **3. .NET Runtime Issues**

```bash
# Verify .NET installation
dotnet --info

# Check runtime versions
dotnet --list-runtimes

# Repair .NET installation if needed
dotnet --list-sdks
```

### **Log Analysis**

```bash
# Check application logs
Get-Content "C:\inetpub\wwwroot\CinemaWebsite\Logs\log-*.txt" | Select-String "ERROR"

# Check Windows Event Logs
Get-EventLog -LogName Application -Source "IIS AspNetCore Module" -Newest 50

# Check IIS logs
Get-Content "C:\inetpub\logs\LogFiles\W3SVC1\*.log" | Select-String "500"
```

## ✅ Production Checklist

### **Pre-Deployment**

- [ ] **Environment Variables** configured
- [ ] **Database** deployed and tested
- [ ] **SSL Certificate** installed
- [ ] **Firewall Rules** configured
- [ ] **Monitoring** tools installed
- [ ] **Backup Strategy** implemented

### **Security**

- [ ] **HTTPS** enforced
- [ ] **Authentication** configured
- [ ] **Authorization** rules set
- [ ] **Input Validation** implemented
- [ ] **SQL Injection** prevention
- [ ] **XSS Protection** enabled

### **Performance**

- [ ] **Caching** configured
- [ ] **Compression** enabled
- [ ] **Static Files** optimized
- [ ] **Database Indexes** created
- [ ] **Connection Pooling** configured

### **Monitoring**

- [ ] **Health Checks** implemented
- [ ] **Logging** configured
- [ ] **Error Tracking** enabled
- [ ] **Performance Metrics** collected
- [ ] **Alerting** configured

### **Backup & Recovery**

- [ ] **Database Backups** scheduled
- [ ] **Application Backups** configured
- [ ] **Disaster Recovery** plan documented
- [ ] **Restore Procedures** tested
- [ ] **Backup Monitoring** implemented

## 📚 Additional Resources

### **Official Documentation**

- [ASP.NET Core Deployment](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/)
- [IIS Configuration](https://docs.microsoft.com/en-us/iis/)
- [SQL Server Deployment](https://docs.microsoft.com/en-us/sql/database-engine/install-windows/)

### **Best Practices**

- [Microsoft Security Development Lifecycle](https://www.microsoft.com/en-us/securityengineering/sdl/)
- [OWASP Deployment Security](https://owasp.org/www-project-deployment-security/)
- [Azure Well-Architected Framework](https://docs.microsoft.com/en-us/azure/architecture/framework/)

---

## 🆘 Support

### **Deployment Issues**

- **GitHub Issues**: [Create an issue](https://github.com/quocdat2601/CinemaWebsiteFPT777/issues)
- **Documentation**: Check the `Document/` folder
- **Team Contact**: Reach out to project maintainers

### **Emergency Contacts**

| Issue Type | Contact | Response Time |
|------------|---------|---------------|
| **Deployment Failure** | [dev-team@domain.com] | 2 hours |
| **Database Issues** | [db-admin@domain.com] | 1 hour |
| **Security Issues** | [security@domain.com] | Immediate |
| **Performance Issues** | [performance@domain.com] | 4 hours |

---

**🚀 Successfully deploying your Cinema Website to production! 🚀**

For additional deployment assistance, please refer to the comprehensive documentation in the `Document/` folder. 