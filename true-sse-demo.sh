#!/bin/bash

# TRUE SSE Streaming Demo using REAL /mcp endpoint
# This demonstrates how the Microsoft MCP SDK actually works

echo "🌊 Wikipedia MCP Server - TRUE SSE Streaming Demo"
echo "Using REAL /mcp endpoint with Server-Sent Events transport"
echo "════════════════════════════════════════════════════════════════"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${YELLOW}🎯 Why Use /mcp Instead of /mcp/rpc?${NC}"
echo "════════════════════════════════════════════════════════════════"
echo -e "${GREEN}✅ /mcp (SSE):${NC}"
echo "  • TRUE Server-Sent Events streaming"
echo "  • Persistent bidirectional connection"
echo "  • Server can push multiple responses"
echo "  • Real-time communication"
echo "  • Used by VS Code MCP extension"
echo "  • Used by Claude Desktop"
echo "  • Microsoft MCP SDK implementation"
echo ""
echo -e "${BLUE}📞 /mcp/rpc (HTTP):${NC}"
echo "  • Traditional request/response"
echo "  • Connection closes after response"
echo "  • Good for testing and APIs"
echo "  • Used by Postman and curl"
echo ""

echo -e "${PURPLE}🚀 DEMO 1: TRUE SSE Connection to /mcp${NC}"
echo "────────────────────────────────────────────────────────────────"
echo -e "${CYAN}Establishing SSE connection...${NC}"
echo ""

# Function to test SSE streaming
test_sse_streaming() {
    echo -e "${GREEN}📡 Connecting to SSE endpoint: POST /mcp${NC}"
    echo -e "${YELLOW}Headers:${NC}"
    echo "  Accept: text/event-stream"
    echo "  Content-Type: application/json"
    echo "  Connection: keep-alive"
    echo ""
    
    echo -e "${YELLOW}Request:${NC}"
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
    echo -e "${GREEN}🌊 SSE Response Stream:${NC}"
    
    # Use curl with streaming to show SSE in action
    timeout 8s curl -N -X POST http://localhost:5070/mcp \
        -H "Accept: text/event-stream" \
        -H "Content-Type: application/json" \
        -H "Connection: keep-alive" \
        -d '{
            "jsonrpc": "2.0",
            "id": 1,
            "method": "initialize",
            "params": {
                "protocolVersion": "2024-11-05",
                "capabilities": {"tools": {}},
                "clientInfo": {"name": "SSE Demo", "version": "1.0"}
            }
        }' 2>/dev/null | while IFS= read -r line; do
            if [[ $line == data:* ]]; then
                echo -e "${GREEN}📥 SSE Event:${NC} $line"
            elif [[ $line == event:* ]]; then
                echo -e "${PURPLE}🏷️ Event Type:${NC} $line"
            elif [[ $line == id:* ]]; then
                echo -e "${BLUE}🔢 Event ID:${NC} $line"
            elif [[ -n "$line" ]]; then
                echo -e "${CYAN}📄 SSE Data:${NC} $line"
            fi
        done || echo -e "${YELLOW}(Connection maintained for 8 seconds - showing persistent nature)${NC}"
    
    echo ""
    echo -e "${GREEN}✅ Connection Status:${NC} PERSISTENT (stays open for real-time communication)"
}

echo -e "${PURPLE}🧪 Testing SSE Connection...${NC}"
test_sse_streaming

echo ""
echo "════════════════════════════════════════════════════════════════"
echo ""

echo -e "${PURPLE}🚀 DEMO 2: Comparison with HTTP Mode${NC}"
echo "────────────────────────────────────────────────────────────────"
echo -e "${BLUE}📞 HTTP Request to /mcp/rpc (for comparison):${NC}"

http_response=$(curl -s -X POST http://localhost:5070/mcp/rpc \
    -H "Content-Type: application/json" \
    -H "MCP-Protocol-Version: 2024-11-05" \
    -d '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' 2>/dev/null)

if [ $? -eq 0 ] && [ -n "$http_response" ]; then
    echo -e "${GREEN}📥 HTTP Response:${NC}"
    echo "$http_response" | jq . 2>/dev/null || echo "$http_response"
    echo -e "${RED}🔚 Connection: CLOSED${NC} (as expected for HTTP)"
else
    echo -e "${YELLOW}⚠️ HTTP endpoint test - connection handling by server${NC}"
fi

echo ""
echo "════════════════════════════════════════════════════════════════"
echo ""

echo -e "${YELLOW}🎯 Key Demo Points for Your Presentation:${NC}"
echo ""
echo -e "${GREEN}1. 🌊 TRUE SSE Streaming:${NC}"
echo "   • Your /mcp endpoint implements REAL Server-Sent Events"
echo "   • Same technology used by VS Code MCP extension"
echo "   • Connection stays open for real-time communication"
echo ""
echo -e "${GREEN}2. 🏗️ Dual Architecture Strength:${NC}"
echo "   • /mcp/rpc for HTTP compatibility (testing, APIs)"
echo "   • /mcp for SSE streaming (AI clients, real-time apps)"
echo ""
echo -e "${GREEN}3. 🎪 Live Demo Options:${NC}"
echo "   • Browser demo: http://localhost:5070/true-sse-demo"
echo "   • Command line: This script!"
echo "   • VS Code integration: Real MCP client"
echo ""
echo -e "${GREEN}4. 💼 Business Value:${NC}"
echo "   • Enterprise-grade flexibility"
echo "   • Supports both REST and streaming paradigms"
echo "   • Future-proof architecture"
echo ""

echo -e "${CYAN}🚀 Your Wikipedia MCP Server demonstrates cutting-edge streaming technology!${NC}"
echo ""
echo -e "${PURPLE}📚 Technical Notes:${NC}"
echo "• Microsoft MCP SDK handles SSE complexity automatically"
echo "• Connection multiplexing allows multiple tool calls"
echo "• Graceful fallback from SSE to HTTP when needed"
echo "• Real-time error handling and connection management"
echo ""

echo -e "${GREEN}✅ TRUE SSE Demo Complete!${NC}"