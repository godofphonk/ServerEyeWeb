#!/bin/bash

# Memory Snapshot with Manual Load Testing
# Starts profiling and waits for you to run load test manually

set -e

# Get script directory and project root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

CONTAINER_NAME="ServerEyeWeb-backend-dev"
SNAPSHOT_DIR="${PROJECT_ROOT}/profiling/snapshots/memory"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
SNAPSHOT_NAME="memory-snapshot-load-${TIMESTAMP}.dmw"
DURATION=${1:-60}  # Default 60 seconds

echo "🚀 Memory Profiling with Manual Load Testing"
echo "================================================"
echo "Container: ${CONTAINER_NAME}"
echo "Duration: ${DURATION} seconds"
echo ""

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

# Start profiling
echo ""
echo "📸 Starting memory profiler (${DURATION}s)..."
echo ""
echo "🔥 START YOUR LOAD TEST IN POSTMAN NOW!"
echo "   Collection: profiling/load-test-collection.json"
echo "   Iterations: 10000-50000"
echo "   Delay: 1ms"
echo ""
echo "⏱️  Profiling for ${DURATION} seconds..."

# Convert seconds to proper format (use seconds directly)
docker exec ${CONTAINER_NAME} sh -c "./dotMemoryclt/tools/dotmemory attach 1 --timeout=${DURATION}s"

# Wait a bit for snapshot to be saved
echo ""
echo "💾 Waiting for snapshot to be saved..."
sleep 3

# Find the latest snapshot file
LATEST_SNAPSHOT=$(docker exec ${CONTAINER_NAME} sh -c "ls -t /app/*.dmw 2>/dev/null | head -1" || echo "")

if [ -z "$LATEST_SNAPSHOT" ]; then
    echo "❌ Error: No snapshot file found"
    echo "💡 Check container logs: docker logs ${CONTAINER_NAME}"
    exit 1
fi

echo "📦 Snapshot created: ${LATEST_SNAPSHOT}"

# Copy snapshot to host
echo "📂 Copying snapshot to ${SNAPSHOT_DIR}/${SNAPSHOT_NAME}..."
mkdir -p "${SNAPSHOT_DIR}"
docker cp "${CONTAINER_NAME}:${LATEST_SNAPSHOT}" "${SNAPSHOT_DIR}/${SNAPSHOT_NAME}"

# Get file size
FILE_SIZE=$(du -h "${SNAPSHOT_DIR}/${SNAPSHOT_NAME}" | cut -f1)

echo ""
echo "✅ Automated memory profiling completed!"
echo "================================================"
echo "📊 Snapshot: ${SNAPSHOT_DIR}/${SNAPSHOT_NAME}"
echo "📏 Size: ${FILE_SIZE}"
echo ""
echo "💡 Open in dotMemory:"
echo "   File → Open → ${SNAPSHOT_DIR}/${SNAPSHOT_NAME}"
