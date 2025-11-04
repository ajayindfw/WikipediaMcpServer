#!/bin/bash

# Wikipedia MCP Server - Local Testing Startup Script
# This script starts the Wikipedia MCP Server for local testing with Postman or other tools

echo "🚀 Starting Wikipedia MCP Server for Local Testing"
echo "=================================================="

# Set script directory as working directory
cd "$(dirname "$0")"

# Check if .NET is available
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK not found. Please install .NET 8.0 SDK"
    echo "   Download from: https://dotnet.microsoft.com/download"
    exit 1
fi

# Check if we're using .NET 8 SDK (due to global.json)
DOTNET_VERSION=$(dotnet --version)
if [[ ! "$DOTNET_VERSION" =~ ^8\. ]]; then
    echo "⚠️  Warning: Using .NET $DOTNET_VERSION instead of .NET 8"
    echo "   global.json should pin to .NET 8.0.406"
fi

# Check if project file exists
if [ ! -f "src/WikipediaMcpServer/WikipediaMcpServer.csproj" ]; then
    echo "❌ Project file not found. Make sure you're in the correct directory."
    exit 1
fi

# Kill any existing instances
echo "🧹 Cleaning up any existing server instances..."
pkill -f "dotnet.*WikipediaMcpServer" 2>/dev/null || true
sleep 2

# Build the project
echo "🔨 Building the project..."
dotnet build src/WikipediaMcpServer/WikipediaMcpServer.csproj
if [ $? -ne 0 ]; then
    echo "❌ Build failed. Please check the build errors above."
    exit 1
fi

echo "✅ Build successful!"
echo ""
echo "🌐 Starting server on http://localhost:5070"
echo "📝 Server logs will be displayed below..."
echo "⏹️  Press Ctrl+C to stop the server"
echo ""
echo "Available endpoints:"
echo "  🏥 Health: http://localhost:5070/health"
echo "  ℹ️  Info:   http://localhost:5070/info"
echo "  📋 Swagger: http://localhost:5070/swagger"
echo "  🔗 MCP JSON-RPC: http://localhost:5070/mcp/rpc"
echo "  🔗 MCP SDK: http://localhost:5070/mcp"
echo ""
echo "Ready for Postman testing! 🚀"
echo "Use WikipediaMcpServer-MCP-JsonRPC-Collection.json for comprehensive testing"
echo ""

# Start the server
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj