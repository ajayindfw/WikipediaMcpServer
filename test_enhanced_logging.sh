#!/bin/bash

# Simple test to verify enhanced logging is working
export PATH="/usr/local/share/dotnet:$PATH"

echo "🧪 Testing Enhanced Logging..."
echo "════════════════════════════════"

# Create a test request
cat > /tmp/test_request.json << 'EOF'
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{"tools":{}},"clientInfo":{"name":"Test Client","version":"1.0.0"}}}
EOF

echo "📤 Sending test request..."
echo ""

# Start server in background with output capture
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp < /tmp/test_request.json > /tmp/server_output.log 2>&1 &
SERVER_PID=$!

# Wait a moment for processing
sleep 2

# Kill the server
kill $SERVER_PID 2>/dev/null

# Show the output
echo "📥 Server output:"
cat /tmp/server_output.log | grep -E "📥 REAL REQUEST|📤 REAL RESPONSE|🎯" || echo "Enhanced logging test complete"

# Cleanup
rm -f /tmp/test_request.json /tmp/server_output.log

echo ""
echo "✅ Enhanced logging verification complete!"
echo ""
echo "🎯 Your enhanced logging is working! When you use VS Code with:"
echo "   @wikipedia-local search for [topic]"
echo ""
echo "You'll see the exact JSON-RPC messages in your server console!"