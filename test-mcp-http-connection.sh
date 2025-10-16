#!/bin/bash

# Test MCP HTTP connection to remote server
# Usage: ./test-mcp-http-connection.sh <render-url>

set -e

RENDER_URL=${1:-"https://your-render-url.onrender.com"}
MCP_ENDPOINT="${RENDER_URL}/api/wikipedia"

echo "🌐 Testing MCP HTTP Connection to Remote Server"
echo "URL: ${MCP_ENDPOINT}"
echo "=============================================="

# Test 1: MCP Initialize
echo -n "Testing MCP Initialize... "
init_response=$(curl -s -X POST \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
      "protocolVersion": "2024-11-05",
      "capabilities": {"tools": {}},
      "clientInfo": {"name": "Test Client", "version": "1.0.0"}
    }
  }' \
  "${MCP_ENDPOINT}" 2>/dev/null)

if echo "$init_response" | grep -q '"result"'; then
  echo "✅ SUCCESS"
else
  echo "❌ FAILED"
  echo "Response: $init_response"
  exit 1
fi

# Test 2: List Tools
echo -n "Testing MCP List Tools... "
tools_response=$(curl -s -X POST \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/list",
    "params": {}
  }' \
  "${MCP_ENDPOINT}" 2>/dev/null)

if echo "$tools_response" | grep -q '"tools"'; then
  echo "✅ SUCCESS"
  echo "Available tools:"
  echo "$tools_response" | grep -o '"name":"[^"]*"' | sed 's/"name":"//g; s/"//g' | sed 's/^/  - /'
else
  echo "❌ FAILED"
  echo "Response: $tools_response"
  exit 1
fi

# Test 3: Call Wikipedia Search Tool
echo -n "Testing Wikipedia Search Tool... "
search_response=$(curl -s -X POST \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 3,
    "method": "tools/call",
    "params": {
      "name": "wikipedia_search",
      "arguments": {
        "query": "artificial intelligence",
        "limit": 3
      }
    }
  }' \
  "${MCP_ENDPOINT}" 2>/dev/null)

if echo "$search_response" | grep -q '"content"'; then
  echo "✅ SUCCESS"
else
  echo "❌ FAILED"
  echo "Response: $search_response"
  exit 1
fi

echo ""
echo "🎉 All MCP HTTP tests passed!"
echo "Your remote MCP server is ready for use."
echo ""
echo "📋 Configuration for your MCP client:"
echo "{"
echo "  \"mcpServers\": {"
echo "    \"wikipedia-remote\": {"
echo "      \"transport\": {"
echo "        \"type\": \"http\","
echo "        \"url\": \"${MCP_ENDPOINT}\""
echo "      }"
echo "    }"
echo "  }"
echo "}"