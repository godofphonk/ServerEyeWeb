#!/bin/bash
# CPU Snapshot with Manual Load Testing (dotTrace CLI)

set -e

# Get script directory and project root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

CONTAINER_NAME="ServerEyeWeb-backend-dev"
SNAPSHOT_DIR="${PROJECT_ROOT}/profiling/snapshots/cpu"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
SNAPSHOT_NAME="cpu-snapshot-load-${TIMESTAMP}.dtp"
DURATION=${1:-60}  # Default 60 seconds

echo ""
echo " CPU Profiling with Manual Load Testing"
echo "================================================"
echo "Container: ${CONTAINER_NAME}"
echo "Duration: ${DURATION} seconds"
echo ""

# Check if container is running
if ! docker ps --format '{{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
    echo " Error: Container ${CONTAINER_NAME} is not running"
    exit 1
fi

# Check if dotTrace CLI is installed in container
echo " Checking dotTrace CLI installation..."
if ! docker exec ${CONTAINER_NAME} test -d ./dotTraceclt/tools; then
    echo " Installing dotTrace CLI..."
    docker exec ${CONTAINER_NAME} sh -c "
        apt-get update -y && apt-get install -y wget unzip && \
        wget -O dotTraceclt.zip https://www.nuget.org/api/v2/package/JetBrains.dotTrace.CommandLineTools.linux-x64 && \
        unzip -q dotTraceclt.zip -d ./dotTraceclt && \
        chmod +x -R dotTraceclt/*
    "
    echo " dotTrace CLI installed"
else
    echo " dotTrace CLI already installed"
fi

# Get dotnet process PID from container
echo " Finding dotnet process in container..."
DOTNET_PID=$(docker exec ${CONTAINER_NAME} pgrep -f 'dotnet.*ServerEye.API.dll' | head -1)

if [ -z "$DOTNET_PID" ]; then
    echo " dotnet process not found in container"
    exit 1
fi

echo " Found dotnet process PID: $DOTNET_PID"

# Start profiling
echo ""
echo " Starting CPU profiler (${DURATION}s)..."
echo ""
echo " START YOUR LOAD TEST IN POSTMAN NOW!"
echo "   Collection: profiling/load-test-collection.json"
echo "   Iterations: 10000-50000"
echo "   Delay: 1ms"
echo ""
echo "  Profiling for ${DURATION} seconds..."

# Cleanup old snapshot files
docker exec ${CONTAINER_NAME} rm -f /tmp/snapshot.dtp* 2>/dev/null || true

# Use unique snapshot name based on timestamp
UNIQUE_SNAPSHOT="/tmp/snapshot-${TIMESTAMP}.dtp"

# Run dotTrace CLI
mkdir -p "${SNAPSHOT_DIR}"
docker exec ${CONTAINER_NAME} sh -c "./dotTraceclt/tools/dottrace attach ${DOTNET_PID} --save-to=${UNIQUE_SNAPSHOT} --timeout=${DURATION}s --profiling-type=Sampling"

# Wait a bit for snapshot to be saved
echo ""
echo " Waiting for snapshot to be saved..."
sleep 3

# Copy snapshot to host (all .dtp files)
echo " Copying snapshot to ${SNAPSHOT_DIR}/..."
docker cp "${CONTAINER_NAME}:${UNIQUE_SNAPSHOT}" "${SNAPSHOT_DIR}/${SNAPSHOT_NAME}"
docker exec ${CONTAINER_NAME} sh -c "ls ${UNIQUE_SNAPSHOT}.* 2>/dev/null" | while read file; do
    filename=$(basename "$file")
    docker cp "${CONTAINER_NAME}:${file}" "${SNAPSHOT_DIR}/${filename}"
done

# Rename files to match main snapshot name
cd "${SNAPSHOT_DIR}"
BASENAME=$(basename "${UNIQUE_SNAPSHOT}")
for file in ${BASENAME}.*; do
    if [ -f "$file" ]; then
        suffix="${file#${BASENAME}.}"
        mv "$file" "${SNAPSHOT_NAME}.${suffix}"
    fi
done

# Cleanup
docker exec ${CONTAINER_NAME} rm -f ${UNIQUE_SNAPSHOT}* 2>/dev/null || true

# Get file size
FILE_SIZE=$(du -h "${SNAPSHOT_DIR}/${SNAPSHOT_NAME}" | cut -f1)

echo ""
echo " Automated CPU profiling completed!"
echo "================================================"
echo " Snapshot: ${SNAPSHOT_DIR}/${SNAPSHOT_NAME}"
echo " Size: ${FILE_SIZE}"
echo ""
echo " Open in dotTrace Desktop:"
echo "   File → Open → ${SNAPSHOT_DIR}/${SNAPSHOT_NAME}"
