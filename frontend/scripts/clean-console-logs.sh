#!/bin/bash

# Remove console.log statements from production code
# This script will be run as part of the build process

echo "🧹 Cleaning console.log statements from production code..."

# Find all TypeScript/JavaScript files and remove console.log (but keep console.warn and console.error)
find app lib components hooks context -type f \( -name "*.ts" -o -name "*.tsx" -o -name "*.js" -o -name "*.jsx" \) -exec sed -i '/console\.log(/d' {} \;

echo "✅ Console.log statements removed from production code"
