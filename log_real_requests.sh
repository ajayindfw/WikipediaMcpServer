#!/bin/bash

# Real JSON-RPC Request Generator and Logger for Wikipedia MCP Server
# This script demonstrates the actual JSON-RPC messages that VS Code would send

export PATH="/usr/local/share/dotnet:$PATH"

echo "🎯 Wikipedia MCP Server - Real JSON-RPC Request Logger"
echo "════════════════════════════════════════════════════════"
echo ""

# Function to send a JSON-RPC request and log it
send_request() {
    local request="$1"
    local description="$2"
    
    echo "📋 $description"
    echo "───────────────────────────────────────────"
    echo "📤 SENDING REQUEST:"
    echo "$request" | jq -c . 2>/dev/null || echo "$request"
    echo ""
    echo "📥 SERVER RESPONSE:"
    
    # Create temporary file for the single-line JSON request
    local temp_file=$(mktemp)
    echo "$request" | jq -c . > "$temp_file" 2>/dev/null || echo "$request" > "$temp_file"
    
    # Send to MCP server and capture output
    timeout 5s dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp < "$temp_file" 2>&1 | \
        grep -E "📥 REAL REQUEST|📤 REAL RESPONSE|🎯|info:|warn:|❌" | head -10
    
    # Cleanup
    rm -f "$temp_file"
    echo ""
    echo "════════════════════════════════════════════════════════"
    echo ""
}

echo "🚀 Starting Real JSON-RPC Request Simulation..."
echo "These are the EXACT messages VS Code sends to your MCP server:"
echo ""

# 1. Initialize Request (VS Code starts connection)
INIT_REQUEST='{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{"tools":{}},"clientInfo":{"name":"VS Code Copilot","version":"1.0.0"}}}'

send_request "$INIT_REQUEST" "1. VS Code Initialize Handshake"

# 2. Tools List Request (VS Code discovers available tools)
TOOLS_LIST_REQUEST='{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}'

send_request "$TOOLS_LIST_REQUEST" "2. VS Code Requests Available Tools"

# 3. Wikipedia Search Tool Call (User types: @wikipedia-local search for AI)
SEARCH_REQUEST='{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"wikipedia_search","arguments":{"query":"artificial intelligence"}}}'

send_request "$SEARCH_REQUEST" "3. User: '@wikipedia-local search for artificial intelligence'"

# 4. Wikipedia Sections Tool Call (User asks for page structure)
SECTIONS_REQUEST='{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"wikipedia_sections","arguments":{"topic":"Machine Learning"}}}'

send_request "$SECTIONS_REQUEST" "4. User: '@wikipedia-local get sections for Machine Learning'"

# 5. Wikipedia Section Content Tool Call (User asks for specific section)
CONTENT_REQUEST='{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"wikipedia_section_content","arguments":{"topic":"Machine Learning","sectionTitle":"Overview"}}}'

send_request "$CONTENT_REQUEST" "5. User: '@wikipedia-local get Overview section of Machine Learning'"

echo "✅ Real JSON-RPC Request Logging Complete!"
echo ""
echo "🎯 Key Insights:"
echo "• These are the EXACT messages VS Code sends to your MCP server"
echo "• Your server processes JSON-RPC 2.0 protocol over stdin/stdout"
echo "• Each request has an 'id' that matches the response"
echo "• VS Code discovers tools via 'tools/list' then calls them via 'tools/call'"
echo ""
echo "🔧 To see live VS Code requests:"
echo "1. Start server: dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp"
echo "2. Use VS Code Copilot Chat: @wikipedia-local search for [topic]"
echo "3. Watch the console for real-time JSON-RPC messages!"