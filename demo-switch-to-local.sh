#!/bin/bash

# Improved Demo Script: Switch to Local development with proper JSON handling
echo "🔥 Switching to Local Wikipedia MCP Server..."

MCP_JSON="$HOME/Library/Application Support/Code/User/mcp.json"

# Create backup
cp "$MCP_JSON" "$MCP_JSON.backup.$(date +%Y%m%d_%H%M%S)"

# Use Python to safely manipulate JSON
cat > /tmp/switch_to_local.py << 'EOF'
import json
import sys

# Read current mcp.json
with open(sys.argv[1], 'r') as f:
    config = json.load(f)

# Remove any existing wikipedia servers
servers_to_remove = ['wikipedia-local', 'wikipedia-render', 'wikipedia-railway', 'wikipedia-http']
for server in servers_to_remove:
    if server in config['servers']:
        del config['servers'][server]

# Add wikipedia-local server
config['servers']['wikipedia-local'] = {
    "command": "/usr/local/share/dotnet/dotnet",
    "args": [
        "run",
        "--project",
        "/Users/ajay.gupta/learning/ai/mcp/wikipedia/WikipediaMcpServer/src/WikipediaMcpServer/WikipediaMcpServer.csproj",
        "--no-launch-profile",
        "--verbosity",
        "quiet",
        "--",
        "--mcp"
    ],
    "type": "stdio",
    "description": "Local Wikipedia MCP Server - Full featured with SSE streaming demos",
    "env": {
        "DOTNET_ENVIRONMENT": "Production",
        "ASPNETCORE_LOGGING__CONSOLE__DISABLECOLORS": "true",
        "ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT": "None"
    }
}

# Write back to file
with open(sys.argv[1], 'w') as f:
    json.dump(config, f, indent=2)

print("Successfully switched to Local server")
EOF

python3 /tmp/switch_to_local.py "$MCP_JSON"
rm /tmp/switch_to_local.py

# Validate JSON
if python3 -m json.tool "$MCP_JSON" > /dev/null 2>&1; then
    echo "✅ Switched to Local development!"
    echo "📍 Current active server: wikipedia-local (🔥 Local with SSE streaming)"
    echo "🔄 Restart VS Code or reload MCP servers for changes to take effect"
    echo ""
    echo "💡 Remember to start your local server:"
    echo "   cd /Users/ajay.gupta/learning/ai/mcp/wikipedia/WikipediaMcpServer"
    echo "   dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj"
else
    echo "❌ JSON validation failed - restoring backup"
    cp "$MCP_JSON.backup.$(date +%Y%m%d_%H%M%S)" "$MCP_JSON"
fi