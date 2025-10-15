#!/bin/bash

# Remote Deployment Test Script for Wikipedia MCP Server
# Usage: ./test-remote-deployment.sh <your-deployment-url>

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Default URL or accept from command line
DEPLOYMENT_URL=${1:-"https://your-deployment-url.com"}

echo -e "${BLUE}🌐 Testing Wikipedia MCP Server Remote Deployment${NC}"
echo -e "${BLUE}URL: ${DEPLOYMENT_URL}${NC}"
echo "================================================="

# Function to test endpoint
test_endpoint() {
    local url=$1
    local method=${2:-"GET"}
    local data=${3:-""}
    local expected_status=${4:-"200"}
    local test_name=$5
    
    echo -n "Testing ${test_name}... "
    
    if [ "$method" = "POST" ] && [ -n "$data" ]; then
        response=$(curl -s -w "HTTPSTATUS:%{http_code};TIME:%{time_total}" \
            -X POST \
            -H "Content-Type: application/json" \
            -d "$data" \
            "$url" 2>/dev/null || echo "HTTPSTATUS:000;TIME:999")
    else
        response=$(curl -s -w "HTTPSTATUS:%{http_code};TIME:%{time_total}" \
            "$url" 2>/dev/null || echo "HTTPSTATUS:000;TIME:999")
    fi
    
    http_code=$(echo "$response" | grep -o "HTTPSTATUS:[0-9]*" | cut -d: -f2)
    time_total=$(echo "$response" | grep -o "TIME:[0-9.]*" | cut -d: -f2)
    body=$(echo "$response" | sed -E 's/HTTPSTATUS:[0-9]*;TIME:[0-9.]*$//')
    
    if [ "$http_code" = "$expected_status" ]; then
        echo -e "${GREEN}✅ PASS${NC} (${time_total}s)"
        return 0
    else
        echo -e "${RED}❌ FAIL${NC} (HTTP $http_code, ${time_total}s)"
        if [ -n "$body" ]; then
            echo "   Response: $body"
        fi
        return 1
    fi
}

# Test Suite
echo -e "\n${YELLOW}🚀 Health Check Tests${NC}"
test_endpoint "$DEPLOYMENT_URL/api/wikipedia/health" "GET" "" "200" "Health Check"
test_endpoint "$DEPLOYMENT_URL/" "GET" "" "200" "Root Endpoint"

echo -e "\n${YELLOW}🔍 Wikipedia API Tests${NC}"
search_data='{"method": "search", "params": {"query": "artificial intelligence"}}'
test_endpoint "$DEPLOYMENT_URL/api/wikipedia" "POST" "$search_data" "200" "Search API"

sections_data='{"method": "sections", "params": {"topic": "Machine Learning"}}'
test_endpoint "$DEPLOYMENT_URL/api/wikipedia" "POST" "$sections_data" "200" "Sections API"

echo -e "\n${YELLOW}🧪 MCP Protocol Tests${NC}"
mcp_init_data='{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
        "protocolVersion": "2024-11-05",
        "capabilities": {"tools": {}},
        "clientInfo": {"name": "Remote Test", "version": "1.0.0"}
    }
}'
test_endpoint "$DEPLOYMENT_URL/api/wikipedia" "POST" "$mcp_init_data" "200" "MCP Initialize"

mcp_tools_data='{"jsonrpc": "2.0", "id": 2, "method": "tools/list", "params": {}}'
test_endpoint "$DEPLOYMENT_URL/api/wikipedia" "POST" "$mcp_tools_data" "200" "MCP List Tools"

# Summary
echo -e "\n${BLUE}=================================================${NC}"
echo -e "${GREEN}✅ Remote deployment test completed!${NC}"
echo -e "${BLUE}If all tests passed, your deployment is working correctly.${NC}"
echo ""
echo -e "${YELLOW}📋 Next Steps:${NC}"
echo "1. Import Postman collections for comprehensive testing"
echo "2. Set up monitoring and alerts"
echo "3. Configure custom domains if needed"
echo "4. Test with real MCP clients"
echo ""
echo -e "${BLUE}📚 Documentation:${NC}"
echo "- REMOTE_TESTING_GUIDE.md - Comprehensive testing guide"
echo "- DEPLOYMENT.md - Deployment instructions"
echo "- WikipediaMcpServer-Remote-Collection.json - Postman tests"