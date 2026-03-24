# Production Dockerfile for Backend (.NET)
# Multi-stage build optimized for production deployment

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Install health check tools
RUN apk add --no-cache curl ca-certificates

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy project files
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["ServerEye.API/ServerEye.API.csproj", "ServerEye.API/"]
COPY ["ServerEye.Core/ServerEye.Core.csproj", "ServerEye.Core/"]
COPY ["ServerEye.Infrastructure/ServerEye.Infrastructure.csproj", "ServerEye.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "ServerEye.API/ServerEye.API.csproj" \
    --runtime alpine-x64 \
    /p:PublishTrimmed=false

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/ServerEye.API"
RUN dotnet build "ServerEye.API.csproj" \
    -c Release \
    -o /app/build \
    --no-restore \
    -p:PublishReadyToRun=true

# Publish stage
FROM build AS publish
RUN dotnet publish "ServerEye.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    --self-contained false \
    -p:PublishTrimmed=false \
    -p:PublishReadyToRun=true

# Production stage
FROM base AS final

# Create non-root user
RUN addgroup -g 1001 -S servereye && \
    adduser -S servereye -u 1001 -G servereye

WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R servereye:servereye /app

# Switch to non-root user
USER servereye

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://127.0.0.1:80/health || exit 1

# Production environment variables
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:80 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_GC_CONCURRENT=true \
    DOTNET_GC_SERVER=true \
    DOTNET_TIEREDCOMPILATION=true

# Start the application
ENTRYPOINT ["dotnet", "ServerEye.API.dll"]
