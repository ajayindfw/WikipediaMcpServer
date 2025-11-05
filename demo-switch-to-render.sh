#!/bin/bash

# Improved Demo Script: Switch to Render deployment with proper JSON handling
echo "🌐 Switching to Wikipedia MCP Server on Render..."

MCP_JSON="$HOME/Library/Application Support/Code/User/mcp.json"

# Create backup
cp "$MCP_JSON" "$MCP_JSON.backup.$(date +%Y%m%d_%H%M%S)"

# Use Python to safely manipulate JSON
cat > /tmp/switch_to_render.py << 'EOF'
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

# Add wikipedia-render server
config['servers']['wikipedia-render'] = {
    "command": "node",
    "args": [
        "/Users/ajay.gupta/learning/ai/mcp/wikipedia/WikipediaMcpServer/mcp-http-bridge.js",
        "render"
    ],
    "type": "stdio",
    "description": "Wikipedia MCP Server on Render - Production cloud deployment",
    "env": {
        "NODE_ENV": "production",
        "MCP_PROVIDER": "render"
    }
}

# Write back to file
with open(sys.argv[1], 'w') as f:
    json.dump(config, f, indent=2)

print("Successfully switched to Render server")
EOF

python3 /tmp/switch_to_render.py "$MCP_JSON"
rm /tmp/switch_to_render.py

# Validate JSON
if python3 -m json.tool "$MCP_JSON" > /dev/null 2>&1; then
    echo "✅ Switched to Render deployment!"
    echo "📍 Current active server: wikipedia-render (☁️ Cloud)"
    echo "🔄 Restart VS Code or reload MCP servers for changes to take effect"
else
    echo "❌ JSON validation failed - restoring backup"
    cp "$MCP_JSON.backup.$(date +%Y%m%d_%H%M%S)" "$MCP_JSON"
fi