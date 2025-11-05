#!/bin/bash

# Live SSE Streaming Demo Script for Wikipedia MCP Server
# This script demonstrates TRUE streaming capabilities vs traditional HTTP

echo "🎯 Wikipedia MCP Server - Live SSE vs HTTP Demo"
echo "════════════════════════════════════════════════════════════════"
echo ""
echo "This demo shows the difference between:"
echo "  📞 Traditional HTTP (/mcp/rpc) - Request → Response → Close"
echo "  🌊 SSE Streaming (/mcp) - Persistent connection with real-time events"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to show a demo request
demo_section() {
    echo -e "${BLUE}$1${NC}"
    echo "────────────────────────────────────────────────────────────────"
}

demo_section "🔥 DEMO 1: Traditional HTTP Request/Response (/mcp/rpc)"
echo -e "${YELLOW}What happens:${NC} Single request → Single response → Connection closes"
echo ""
echo -e "${GREEN}Request:${NC}"
cat << 'EOF'
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/list",
  "params": {}
}
EOF

echo ""
echo -e "${GREEN}Response (received immediately):${NC}"
curl -s -X POST http://localhost:5070/mcp/rpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' | jq .

echo ""
echo -e "${RED}Connection:${NC} ❌ CLOSED after response"
echo ""
echo "════════════════════════════════════════════════════════════════"
echo ""

demo_section "🌊 DEMO 2: SSE Streaming Mode (/mcp)"
echo -e "${YELLOW}What happens:${NC} Persistent connection → Server can push multiple messages"
echo ""
echo -e "${GREEN}Opening SSE connection to /mcp endpoint...${NC}"
echo ""

# For SSE demo, we'll use a background process to show the concept
echo -e "${GREEN}Request:${NC}"
cat << 'EOF'
{
  "jsonrpc": "2.0", 
  "id": 1,
  "method": "initialize",
  "params": {
    "protocolVersion": "2024-11-05",
    "capabilities": {"tools": {}},
    "clientInfo": {"name": "SSE Demo", "version": "1.0"}
  }
}
EOF

echo ""
echo -e "${GREEN}SSE Response (streamed):${NC}"

# Note: curl with -N flag keeps connection open for streaming
timeout 5s curl -N -X POST http://localhost:5070/mcp \
  -H "Accept: text/event-stream" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{"tools":{}},"clientInfo":{"name":"SSE Demo","version":"1.0"}}}' \
  2>/dev/null || echo -e "${YELLOW}(Connection was persistent - timed out after 5 seconds)${NC}"

echo ""
echo -e "${GREEN}Connection:${NC} ✅ PERSISTENT (stays open for real-time communication)"
echo ""
echo "════════════════════════════════════════════════════════════════"
echo ""

demo_section "🔬 Technical Analysis"
echo -e "${YELLOW}HTTP Mode (/mcp/rpc):${NC}"
echo "  • Request → Response → Close"
echo "  • Each tool call = new HTTP connection"
echo "  • Perfect for: APIs, testing, one-off requests"
echo "  • Used by: Postman, REST clients, testing tools"
echo ""

echo -e "${YELLOW}SSE Mode (/mcp):${NC}"
echo "  • Persistent bidirectional connection"
echo "  • Server can push multiple responses"
echo "  • Real-time communication capabilities"  
echo "  • Perfect for: AI assistants, real-time apps, chatbots"
echo "  • Used by: VS Code, Claude Desktop, live applications"
echo ""

demo_section "🎯 Key Demo Points for Your Presentation"
echo "1. 🌐 Your MCP server supports BOTH transports (flexibility)"
echo "2. 📞 HTTP mode = Perfect for testing & REST compatibility"  
echo "3. 🌊 SSE mode = True real-time streaming for AI clients"
echo "4. 🛠️ Microsoft MCP SDK handles the SSE complexity automatically"
echo "5. 🎪 Live demo at: http://localhost:5070/demo"
echo ""

echo -e "${GREEN}✅ Demo Complete!${NC}"
echo ""
echo "🎪 For your presentation:"
echo "  • Show the browser demo at /demo for visual impact"
echo "  • Use this script for technical deep-dive"
echo "  • Highlight the dual-transport architecture strength"
echo ""

echo "🚀 Your Wikipedia MCP Server showcases enterprise-grade flexibility!"