# Production Dockerfile for Backend (.NET)
# Optimized for production with multi-stage build

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for better caching
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["backend/ServerEyeBackend/ServerEye.API/ServerEye.API.csproj", "ServerEye.API/"]
COPY ["backend/ServerEyeBackend/ServerEye.Core/ServerEye.Core.csproj", "ServerEye.Core/"]
COPY ["backend/ServerEyeBackend/ServerEye.Infrastructure/ServerEye.Infrastructure.csproj", "ServerEye.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "ServerEye.API/ServerEye.API.csproj" --no-cache

# Copy source code after dependencies are restored
COPY backend/ServerEyeBackend .

# Build the application
WORKDIR "/src/ServerEye.API"
RUN dotnet build "ServerEye.API.csproj" -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "ServerEye.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Production stage
FROM base AS final
WORKDIR /app

# Install curl for health checks and Doppler CLI
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl gnupg && \
    curl -sLf --retry 3 --tlsv1.2 --proto "=https" \
        'https://packages.doppler.com/public/cli/gpg.DE2A7741A397C129.key' \
        | gpg --dearmor -o /usr/share/keyrings/doppler-archive-keyring.gpg && \
    echo "deb [signed-by=/usr/share/keyrings/doppler-archive-keyring.gpg] \
        https://packages.doppler.com/public/cli/deb/debian any-version main" \
        | tee /etc/apt/sources.list.d/doppler-cli.list && \
    apt-get update && \
    apt-get install -y doppler && \
    rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/*

# Copy published application
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://127.0.0.1:8080/health || exit 1

# Production environment variables
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_GC_HEAP_COUNT=2 \
    DOTNET_GCServer=1 \
    DOTNET_ThreadPool_MinThreads=4 \
    DOTNET_ThreadPool_MaxThreads=32

# Start the application with Doppler secrets injection
ENTRYPOINT ["doppler", "run", "--", "dotnet", "ServerEye.API.dll"]
