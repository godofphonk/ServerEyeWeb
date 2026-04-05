#!/bin/bash

# CodeQL Security Analysis Script
# Usage: ./run-codeql-analysis.sh

set -e

echo "🔍 Starting CodeQL Security Analysis..."

# Configuration
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CODEQL_CLI="$HOME/.local/share/gh/extensions/gh-codeql/dist/release/v2.25.1/codeql"
CODEQL_REPO="/tmp/codeql"
DATABASE_NAME="codeql-analysis/servereye-db"
SOURCE_ROOT="backend/ServerEyeBackend"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}📂 Project Root: $PROJECT_ROOT${NC}"
echo -e "${BLUE}🔧 CodeQL CLI: $CODEQL_CLI${NC}"
echo -e "${BLUE}📁 Source Root: $SOURCE_ROOT${NC}"

# Check if CodeQL CLI exists
if [ ! -f "$CODEQL_CLI" ]; then
    echo -e "${RED}❌ CodeQL CLI not found. Please install it first:${NC}"
    echo "gh extension install github/gh-codeql"
    exit 1
fi

# Check if CodeQL repository exists
if [ ! -d "$CODEQL_REPO" ]; then
    echo -e "${YELLOW}📥 Cloning CodeQL repository...${NC}"
    git clone https://github.com/github/codeql.git "$CODEQL_REPO"
else
    echo -e "${GREEN}✅ CodeQL repository already exists${NC}"
fi

# Clean previous database
if [ -d "$PROJECT_ROOT/$DATABASE_NAME" ]; then
    echo -e "${YELLOW}🧹 Cleaning previous database...${NC}"
    rm -rf "$PROJECT_ROOT/$DATABASE_NAME"
fi

# Create CodeQL database
echo -e "${BLUE}🏗️  Creating CodeQL database...${NC}"
cd "$PROJECT_ROOT"
$CODEQL_CLI database create "$DATABASE_NAME" \
    --language=csharp \
    --source-root="$SOURCE_ROOT"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Database created successfully${NC}"
else
    echo -e "${RED}❌ Failed to create database${NC}"
    exit 1
fi

# Run security analysis
echo -e "${BLUE}🔍 Running security analysis...${NC}"
RESULTS_FILE="codeql-analysis/codeql-security-results-$(date +%Y%m%d-%H%M%S).csv"

$CODEQL_CLI database analyze "$DATABASE_NAME" \
    "$CODEQL_REPO/csharp/ql/src/codeql-suites/csharp-security-extended.qls" \
    --format=csv \
    --output="$RESULTS_FILE"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Analysis completed successfully${NC}"
    echo -e "${BLUE}📊 Results saved to: $RESULTS_FILE${NC}"
    
    # Generate summary
    TOTAL_ISSUES=$(wc -l < "$RESULTS_FILE")
    LOG_INJECTION=$(grep '"Log entries created from user input' "$RESULTS_FILE" | wc -l)
    PRIVATE_INFO=$(grep '"Exposure of private information' "$RESULTS_FILE" | wc -l)
    BYPASS=$(grep '"User-controlled bypass of sensitive method' "$RESULTS_FILE" | wc -l)
    REDIRECT=$(grep '"URL redirection from remote source' "$RESULTS_FILE" | wc -l)
    
    echo ""
    echo -e "${BLUE}📈 Analysis Summary:${NC}"
    echo -e "${YELLOW}Total Issues: $TOTAL_ISSUES${NC}"
    echo -e "${RED}🔴 Log Injection: $LOG_INJECTION${NC}"
    echo -e "${YELLOW}🟡 Private Information Exposure: $PRIVATE_INFO${NC}"
    echo -e "${RED}🔴 User-Controlled Bypass: $BYPASS${NC}"
    echo -e "${YELLOW}🟡 URL Redirection: $REDIRECT${NC}"
    
    # Show top 5 issues
    echo ""
    echo -e "${BLUE}🔍 Top 5 Critical Issues:${NC}"
    grep '"Log entries created from user input\|"User-controlled bypass of sensitive method' "$RESULTS_FILE" | head -5 | while IFS= read -r line; do
        FILE=$(echo "$line" | cut -d',' -f5)
        LINE=$(echo "$line" | cut -d',' -f7)
        echo -e "${RED}📍 $FILE:$LINE${NC}"
    done
    
else
    echo -e "${RED}❌ Analysis failed${NC}"
    exit 1
fi

echo ""
echo -e "${GREEN}🎉 CodeQL analysis completed!${NC}"
echo -e "${BLUE}💡 To view detailed results: cat $RESULTS_FILE${NC}"
echo -e "${BLUE}💡 To re-run analysis: ./run-codeql-analysis.sh${NC}"
