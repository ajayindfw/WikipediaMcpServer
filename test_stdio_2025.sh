#!/bin/bash

# Test script for Wikipedia MCP Server stdio mode - Protocol 2025-06-18
export PATH="/usr/local/share/dotnet:$PATH"

echo "🧪 Testing Wikipedia MCP Server in stdio mode (Protocol 2025-06-18)..."
#!/bin/bash

# Test script for Wikipedia MCP Server stdio mode - Protocol 2025-06-18
export PATH="/usr/local/share/dotnet:$PATH"

echo "🧪 Testing Wikipedia MCP Server in stdio mode (Protocol 2025-06-18)..."

# Start the server in background and capture PID
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp &
SERVER_PID=$!

echo "📡 Server started with PID: $SERVER_PID"
sleep 3

# Test initialize with 2025-06-18 protocol
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"Test Client","version":"1.0.0"}}}' > /tmp/mcp_test_input_2025

# Test tools/list  
echo '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' >> /tmp/mcp_test_input_2025

# Test wikipedia search
echo '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"wikipedia_search","arguments":{"query":"artificial intelligence"}}}' >> /tmp/mcp_test_input_2025

# Send commands to server
cat /tmp/mcp_test_input_2025 | dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp

# Cleanup
kill $SERVER_PID 2>/dev/null
rm /tmp/mcp_test_input_2025
echo "✅ Test completed"

echo "🧪 Testing Wikipedia MCP Server in stdio mode (Protocol 2025-06-18)..."
echo "� This tests the enhanced protocol features and capabilities"
echo ""

# Create test input file with 2025-06-18 protocol requests
cat << 'EOF' > /tmp/mcp_test_input_2025
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{"tools":{},"resources":{},"prompts":{}},"clientInfo":{"name":"Test Client 2025","version":"2.0.0"}}}
{"jsonrpc":"2.0","method":"notifications/initialized"}
{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}
{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"wikipedia_search","arguments":{"query":"quantum computing"}}}
{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"wikipedia_sections","arguments":{"topic":"Machine Learning"}}}
{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"wikipedia_section_content","arguments":{"topic":"Artificial Intelligence","section_title":"Overview"}}}
EOF

echo "🔍 Protocol 2025-06-18 Test Sequence:"
echo "  1. Initialize with enhanced capabilities"
echo "  2. Send initialized notification"
echo "  3. List available tools"
echo "  4. Search for 'quantum computing'"
echo "  5. Get sections for 'Machine Learning'"
echo "  6. Get 'Overview' section from 'Artificial Intelligence'"
echo ""

# Send commands to server and capture output (macOS compatible)
echo "📤 Sending requests to server..."

# Test with a simple command first to see if server responds
echo "🔍 Testing server startup..."
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{"tools":{}},"clientInfo":{"name":"Test","version":"1.0"}}}' | dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp > /tmp/mcp_test_stdout_2025 2> /tmp/mcp_test_stderr_2025

echo "📊 Checking server output streams..."
echo "STDOUT size: $(wc -c /tmp/mcp_test_stdout_2025 2>/dev/null | awk '{print $1}') bytes"
echo "STDERR size: $(wc -c /tmp/mcp_test_stderr_2025 2>/dev/null | awk '{print $1}') bytes"

if [ -f /tmp/mcp_test_stdout_2025 ] && [ -s /tmp/mcp_test_stdout_2025 ]; then
    echo ""
    echo "📤 STDOUT (JSON-RPC Responses):"
    echo "─────────────────────────────"
    cat /tmp/mcp_test_stdout_2025
    echo ""
fi

if [ -f /tmp/mcp_test_stderr_2025 ] && [ -s /tmp/mcp_test_stderr_2025 ]; then
    echo ""
    echo "📤 STDERR (Debug Output):"
    echo "─────────────────────────"
    head -20 /tmp/mcp_test_stderr_2025
    echo ""
fi

# Now run the full test
echo "📤 Running full test sequence..."
cat /tmp/mcp_test_input_2025 | dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp > /tmp/mcp_test_output_2025 2> /tmp/mcp_test_debug_2025

echo ""
echo "📥 Server Response Analysis:"
echo "─────────────────────────────"

# Analyze the responses
if [ -f /tmp/mcp_test_output_2025 ] && [ -s /tmp/mcp_test_output_2025 ]; then
    echo ""
    echo "📋 Full Test JSON-RPC Responses:"
    echo "─────────────────────────────────"
    cat /tmp/mcp_test_output_2025
    echo ""
    
    # Count successful responses
    INIT_RESPONSE=$(grep -c '"protocolVersion":"2025-06-18"' /tmp/mcp_test_output_2025 || echo "0")
    TOOLS_LIST=$(grep -c '"tools":\[' /tmp/mcp_test_output_2025 || echo "0")
    SEARCH_RESULT=$(grep -c '"quantum computing"' /tmp/mcp_test_output_2025 || echo "0")
    SECTIONS_RESULT=$(grep -c '"sections"' /tmp/mcp_test_output_2025 || echo "0")
    CONTENT_RESULT=$(grep -c '"content"' /tmp/mcp_test_output_2025 || echo "0")
    
    echo "✅ Initialize (2025-06-18): $INIT_RESPONSE responses"
    echo "✅ Tools List: $TOOLS_LIST responses" 
    echo "✅ Search Results: $SEARCH_RESULT matches"
    echo "✅ Section Results: $SECTIONS_RESULT matches"
    echo "✅ Content Results: $CONTENT_RESULT matches"
    
    # Check for enhanced capabilities
    if grep -q '"listChanged":true' /tmp/mcp_test_output_2025; then
        echo "🆕 Enhanced tools capability: listChanged=true ✅"
    else
        echo "🆕 Enhanced tools capability: listChanged=false"
    fi
    
    if grep -q '"resources":{}' /tmp/mcp_test_output_2025; then
        echo "🆕 Resources capability declared ✅"
    fi
    
    if grep -q '"prompts":{}' /tmp/mcp_test_output_2025; then
        echo "🆕 Prompts capability declared ✅"
    fi
else
    echo "❌ No JSON-RPC responses captured in stdout"
    
    if [ -f /tmp/mcp_test_debug_2025 ] && [ -s /tmp/mcp_test_debug_2025 ]; then
        echo ""
        echo "🔍 Debug output (stderr) - first 30 lines:"
        echo "─────────────────────────────────────────"
        head -30 /tmp/mcp_test_debug_2025
    fi
fi

# Cleanup
rm -f /tmp/mcp_test_input_2025 /tmp/mcp_test_output_2025 /tmp/mcp_test_debug_2025 /tmp/mcp_test_stdout_2025 /tmp/mcp_test_stderr_2025

echo ""
echo "🎯 Protocol Comparison Summary:"
echo "────────────────────────────────"
echo "• 2024-11-05: Basic tools capability only"
echo "• 2025-06-18: Enhanced tools + resources + prompts capabilities"
echo "• Both protocols use same tool implementations"
echo "• Enhanced protocol enables future feature extensions"
echo ""
echo "✅ Protocol 2025-06-18 test completed"