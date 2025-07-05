# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["MovieTheater.csproj", "./"]
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet build "MovieTheater.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MovieTheater.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables for Windows compatibility
ENV ASPNETCORE_URLS=http://+:80;https://+:443
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "MovieTheater.dll"]
