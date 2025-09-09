# Multi-stage build for .NET 8.0 application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["coptic_app_backend.Api/coptic_app_backend.Api.csproj", "coptic_app_backend.Api/"]
COPY ["coptic_app_backend.Application/coptic_app_backend.Application.csproj", "coptic_app_backend.Application/"]
COPY ["coptic_app_backend.Domain/coptic_app_backend.Domain.csproj", "coptic_app_backend.Domain/"]
COPY ["coptic_app_backend.Infrastructure/coptic_app_backend.Infrastructure.csproj", "coptic_app_backend.Infrastructure/"]

RUN dotnet restore "coptic_app_backend.Api/coptic_app_backend.Api.csproj"

# Copy source code
COPY . .
WORKDIR "/src/coptic_app_backend.Api"

# Build the application
RUN dotnet build "coptic_app_backend.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "coptic_app_backend.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create uploads and firebase directories
RUN mkdir -p wwwroot/uploads && chmod 755 wwwroot/uploads
RUN mkdir -p firebase && chmod 755 firebase

# Note: Firebase credential files will be mounted/copied during deployment via CI/CD
# The actual files are created by GitHub Actions from secrets

ENTRYPOINT ["dotnet", "coptic_app_backend.Api.dll"]
