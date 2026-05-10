#!/bin/bash

# Memory Snapshot Script for ServerEye Backend
# Automatically creates memory snapshot and moves it to snapshots directory

set -e

CONTAINER_NAME="ServerEyeWeb-backend-dev"
SNAPSHOT_DIR="/home/gospodin/Desktop/homeProjects/ServerEyeProjects/ServerEyeWeb/profiling/snapshots/memory"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
SNAPSHOT_NAME="memory-snapshot-${TIMESTAMP}.dmw"

echo "🔍 Starting memory profiling for ${CONTAINER_NAME}..."

# Check if container is running
if ! docker ps --format '{{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
    echo "❌ Error: Container ${CONTAINER_NAME} is not running"
    exit 1
fi

# Check if dotMemory CLI is installed in container
echo "📦 Checking dotMemory CLI installation..."
if ! docker exec ${CONTAINER_NAME} test -d ./dotMemoryclt/tools; then
    echo "📥 Installing dotMemory CLI..."
    docker exec ${CONTAINER_NAME} sh -c "
        apt-get update -y && apt-get install -y wget unzip && \
        wget -O dotMemoryclt.zip https://www.nuget.org/api/v2/package/JetBrains.dotMemory.Console.linux-x64 && \
        unzip -q dotMemoryclt.zip -d ./dotMemoryclt && \
        chmod +x -R dotMemoryclt/*
    "
    echo "✅ dotMemory CLI installed"
else
    echo "✅ dotMemory CLI already installed"
fi

# Create snapshot
echo "📸 Creating memory snapshot..."
docker exec ${CONTAINER_NAME} sh -c "./dotMemoryclt/tools/dotmemory get-snapshot 1"

# Find the latest snapshot file
LATEST_SNAPSHOT=$(docker exec ${CONTAINER_NAME} sh -c "ls -t /app/*.dmw | head -1")

if [ -z "$LATEST_SNAPSHOT" ]; then
    echo "❌ Error: No snapshot file found"
    exit 1
fi

echo "📦 Snapshot created: ${LATEST_SNAPSHOT}"

# Copy snapshot to host
echo "📂 Copying snapshot to ${SNAPSHOT_DIR}/${SNAPSHOT_NAME}..."
mkdir -p "${SNAPSHOT_DIR}"
docker cp "${CONTAINER_NAME}:${LATEST_SNAPSHOT}" "${SNAPSHOT_DIR}/${SNAPSHOT_NAME}"

# Get file size
FILE_SIZE=$(du -h "${SNAPSHOT_DIR}/${SNAPSHOT_NAME}" | cut -f1)

echo "✅ Memory snapshot completed!"
echo "📊 Snapshot saved: ${SNAPSHOT_DIR}/${SNAPSHOT_NAME}"
echo "📏 Size: ${FILE_SIZE}"
echo ""
echo "💡 Open in dotMemory: File → Open → ${SNAPSHOT_DIR}/${SNAPSHOT_NAME}"
