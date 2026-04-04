#!/bin/bash

echo "🚀 Локальный CI Pipeline для ServerEye Backend"
echo "=========================================="

cd backend/ServerEyeBackend

echo "1. 🧹 Очистка NuGet кеша..."
dotnet nuget locals all --clear

echo "2. 📦 Восстановление зависимостей..."
dotnet restore

echo "3. 🎨 Проверка форматирования кода..."
dotnet format ServerEyeBackend.sln --verify-no-changes --verbosity diagnostic
if [ $? -eq 0 ]; then
    echo "✅ Форматирование корректное"
else
    echo "⚠️  Форматирование исправлено"
    dotnet format ServerEyeBackend.sln
fi

echo "4. 🔨 Сборка проекта..."
dotnet build ServerEyeBackend.sln --no-restore -c Release /p:EnableNETAnalyzers=false /p:RunAnalyzersDuringBuild=false

echo "5. 🧪 Запуск Unit Tests..."
dotnet test ServerEye.UnitTests/ServerEye.UnitTests.csproj --no-build -c Release --logger "console;verbosity=normal" --no-restore

echo "6. 🔗 Запуск Integration Tests (если Docker доступен)..."
if command -v docker &> /dev/null; then
    dotnet test ServerEye.IntegrationTests/ServerEye.IntegrationTests.csproj --no-build -c Release --logger "console;verbosity=normal" --no-restore
else
    echo "⚠️  Docker недоступен, пропускаем Integration Tests"
fi

echo "✅ Локальный CI завершен!"
