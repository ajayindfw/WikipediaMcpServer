#!/bin/bash

# Test script for Wikipedia MCP Server stdio mode
export PATH="/usr/local/share/dotnet:$PATH"

echo "🧪 Testing Wikipedia MCP Server in stdio mode..."

# Start the server in background and capture PID
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp &
SERVER_PID=$!

echo "📡 Server started with PID: $SERVER_PID"
sleep 3

# Test initialize
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"Test Client","version":"1.0.0"}}}' > /tmp/mcp_test_input

# Test tools/list  
echo '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' >> /tmp/mcp_test_input

# Test wikipedia search
echo '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"wikipedia_search","arguments":{"query":"artificial intelligence"}}}' >> /tmp/mcp_test_input

# Send commands to server
cat /tmp/mcp_test_input | dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp

# Cleanup
kill $SERVER_PID 2>/dev/null
rm /tmp/mcp_test_input
echo "✅ Test completed"