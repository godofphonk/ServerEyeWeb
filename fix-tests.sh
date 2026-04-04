#!/bin/bash

echo "🔧 Исправление fully qualified names в Unit Tests..."

cd backend/ServerEyeBackend/ServerEye.UnitTests

# Исправляем ServerEyeDbContext
find . -name "*.cs" -type f -exec sed -i 's/ServerEyeDbContext/ServerEye\.Infrastructure\.ServerEyeDbContext/g' {} \;

# Исправляем UserRepository
find . -name "*.cs" -type f -exec sed -i 's/UserRepository/ServerEye\.Infrastructure\.Repositories\.UserRepository/g' {} \;

# Исправляем RefreshTokenRepository
find . -name "*.cs" -type f -exec sed -i 's/RefreshTokenRepository/ServerEye\.Infrastructure\.Repositories\.Auth\.RefreshTokenRepository/g' {} \;

# Исправляем GlobalExceptionHandler
find . -name "*.cs" -type f -exec sed -i 's/GlobalExceptionHandler/ServerEye\.API\.Middleware\.GlobalExceptionHandler/g' {} \;

# Исправляем GoApiClient
find . -name "*.cs" -type f -exec sed -i 's/GoApiClient/ServerEye\.Infrastructure\.ExternalServices\.GoApi\.GoApiClient/g' {} \;

# Исправляем GoApiLogger
find . -name "*.cs" -type f -exec sed -i 's/GoApiLogger/ServerEye\.Infrastructure\.ExternalServices\.GoApi\.Logging\.GoApiLogger/g' {} \;

# Исправляем GoApiHttpHandler
find . -name "*.cs" -type f -exec sed -i 's/GoApiHttpHandler/ServerEye\.Infrastructure\.ExternalServices\.GoApi\.GoApiHttpHandler/g' {} \;

# Исправляем GoApiOperationFactory
find . -name "*.cs" -type f -exec sed -i 's/GoApiOperationFactory/ServerEye\.Infrastructure\.ExternalServices\.GoApi\.Operations\.Base\.GoApiOperationFactory/g' {} \;

# Исправляем валидаторы (оставляем как есть, они в правильном namespace)
# Validators уже находятся в ServerEye.API.Validators

# Исправляем Program в Integration Tests
cd ../ServerEye.IntegrationTests
find . -name "*.cs" -type f -exec sed -i 's/: Program/: ServerEye\.API\.Program/g' {} \;

echo "✅ Исправления завершены"
