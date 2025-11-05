#!/bin/bash

# Demo Script: Show current MCP configuration status
echo "📊 Current Wikipedia MCP Server Configuration Status"
echo "=================================================="

MCP_JSON="$HOME/Library/Application Support/Code/User/mcp.json"

if [ ! -f "$MCP_JSON" ]; then
    echo "❌ MCP configuration file not found!"
    exit 1
fi

echo ""
echo "🔍 Checking active Wikipedia servers..."
echo ""

# Check which servers are active (not commented)
if grep -q '^[[:space:]]*"wikipedia-local":' "$MCP_JSON"; then
    echo "🔥 [ACTIVE] wikipedia-local - Local development with SSE streaming"
else
    echo "💤 [INACTIVE] wikipedia-local - (commented out)"
fi

if grep -q '^[[:space:]]*"wikipedia-render":' "$MCP_JSON"; then
    echo "🌐 [ACTIVE] wikipedia-render - Render cloud deployment"
else
    echo "💤 [INACTIVE] wikipedia-render - (commented out)"
fi

if grep -q '^[[:space:]]*"wikipedia-railway":' "$MCP_JSON"; then
    echo "🚂 [ACTIVE] wikipedia-railway - Railway cloud deployment"
else
    echo "💤 [INACTIVE] wikipedia-railway - (commented out)"
fi

if grep -q '^[[:space:]]*"wikipedia-http":' "$MCP_JSON"; then
    echo "🔗 [ACTIVE] wikipedia-http - HTTP bridge mode"
else
    echo "💤 [INACTIVE] wikipedia-http - (commented out)"
fi

echo ""
echo "📁 Configuration file: $MCP_JSON"
echo ""
echo "🎯 Demo Scripts Available:"
echo "   ./demo-switch-to-local.sh   - Switch to local development"
echo "   ./demo-switch-to-render.sh  - Switch to Render cloud"
echo "   ./demo-switch-to-railway.sh - Switch to Railway cloud"
echo "   ./demo-status.sh            - Show this status"
echo ""
echo "🔄 After switching, restart VS Code or reload MCP servers"