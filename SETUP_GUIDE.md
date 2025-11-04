# Wikipedia MCP Server - Complete Setup Guide

This comprehensive guide covers all setup scenarios for the Wikipedia MCP Server, from local development to remote deployment and client configuration.

## 📋 Prerequisites

- .NET 8.0 SDK
- Visual Studio Code (for MCP integration)
- Node.js (for remote bridge functionality)
- Git

## 🚀 Quick Start

### Local Development (Recommended for beginners)

```bash
# 1. Clone and build
git clone <repository-url>
cd WikipediaMcpServer
dotnet restore
dotnet build src/WikipediaMcpServer/WikipediaMcpServer.csproj

# 2. Run in stdio mode (for AI clients)
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp

# 3. Or run in HTTP mode (for testing)
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj
```

---

## 🎯 Transport Modes Overview

The Wikipedia MCP Server supports three transport modes:

| **Transport** | **Use Case** | **Command** | **Best For** |
|---------------|-------------|-------------|-------------|
| **stdio Mode** | Local AI clients | `-- --mcp` | VS Code, Claude Desktop |
| **HTTP RPC Mode** | Remote access | Default + `/mcp/rpc` | Teams, remote deployment |
| **HTTP SDK Mode** | Advanced clients | Default + `/mcp` | SSE/WebSocket clients |

---

## 📟 Local Development (stdio Mode)

### What is stdio Mode?

stdio (standard input/output) mode enables direct communication with AI clients:
- **stdin** - Reads JSON-RPC 2.0 requests from standard input
- **stdout** - Writes JSON-RPC 2.0 responses to standard output
- **stderr** - Logs diagnostic messages without interfering with protocol

### Benefits of stdio Mode

- 🔐 **Secure** - No network ports exposed
- 🚀 **Fast** - Direct process communication
- 🎯 **Simple** - No HTTP/SSE overhead
- 🔧 **Debuggable** - Clean separation of logs and protocol

### Starting stdio Mode

```bash
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp
```

### VS Code Integration

#### Method 1: Global Configuration (Recommended)

Edit `~/Library/Application Support/Code/User/mcp.json` (macOS) or equivalent:

```json
{
  "mcpServers": {
    "wikipedia-local": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/absolute/path/to/src/WikipediaMcpServer/WikipediaMcpServer.csproj",
        "--",
        "--mcp"
      ],
      "env": {
        "DOTNET_ENVIRONMENT": "Development"
      },
      "description": "Local Wikipedia MCP Server"
    }
  }
}
```

#### Method 2: Workspace Configuration

Add to `.vscode/settings.json`:

```json
{
  "mcp.servers": {
    "wikipedia": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "${workspaceFolder}/src/WikipediaMcpServer/WikipediaMcpServer.csproj",
        "--",
        "--mcp"
      ]
    }
  }
}
```

#### Reload VS Code

After configuration:
1. Press `Cmd+Shift+P` (macOS) or `Ctrl+Shift+P` (Windows/Linux)
2. Run: "Developer: Reload Window"
3. Check status bar for MCP connection indicator

### Claude Desktop Integration

Add to Claude Desktop configuration file:

**macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
**Windows**: `%APPDATA%\Claude\claude_desktop_config.json`

```json
{
  "mcpServers": {
    "wikipedia": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/absolute/path/to/src/WikipediaMcpServer/WikipediaMcpServer.csproj",
        "--",
        "--mcp"
      ]
    }
  }
}
```

---

## 🌐 Remote Deployment

### Overview

The Wikipedia MCP Server supports **BOTH local (stdio) and remote (HTTP) MCP access** simultaneously!

### Supported Remote Transport Methods

1. **HTTP RPC Mode** - JSON-RPC over HTTP POST (`/mcp/rpc`)
2. **HTTP SDK Mode** - Microsoft MCP SDK with SSE/WebSocket (`/mcp`)

### Already Deployed Server

The Wikipedia MCP Server is running on Render at:
```
https://wikipediamcpserver.onrender.com
```

**Available Endpoints:**
- `/health` - Health check endpoint
- `/info` - Server information and available endpoints
- `/mcp/rpc` - Remote MCP JSON-RPC endpoint
- `/mcp` - Microsoft MCP SDK endpoint (SSE/WebSocket)

### Testing Remote Server

```bash
# Health check
curl https://wikipediamcpserver.onrender.com/health

# Server info (shows all endpoints)
curl https://wikipediamcpserver.onrender.com/info

# Test MCP RPC endpoint
curl -X POST https://wikipediamcpserver.onrender.com/mcp/rpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}'
```

### Deploy Your Own Instance on Render

1. **Fork this repository** to your GitHub account
2. **Create a new Web Service** on [Render](https://render.com)
3. **Connect your GitHub repository** to Render
4. **Use automatic configuration** via `render.yaml`
5. **Deploy** with one click!

**Manual Configuration (if not using render.yaml):**
- Build Command: `dotnet publish src/WikipediaMcpServer/WikipediaMcpServer.csproj -c Release -o out`
- Start Command: `dotnet out/WikipediaMcpServer.dll`
- Environment: Select ".NET"
- Add environment variable: `ASPNETCORE_URLS=http://0.0.0.0:$PORT`

### Deploy Your Own Instance on Railway

1. **Fork this repository** to your GitHub account
2. **Create a new Project** on [Railway](https://railway.com)
3. **Connect your GitHub repository** to Railway
4. **Use automatic configuration** via `railway.json`
5. **Deploy** with zero configuration!

**Automatic Configuration Features:**
- Uses `railway.json` for build and deploy settings with Dockerfile builder
- Uses official Microsoft .NET 8 Docker images for reliable builds
- Includes health checks, restart policies, and environment variables
- Compatible with `global.json` SDK version requirements
- No dependency on Nixpacks or third-party package managers

**Manual Configuration (if not using railway.json):**
- Builder: Select "Dockerfile"
- Dockerfile Path: `Dockerfile` (default)
- Add environment variables:
  - `ASPNETCORE_ENVIRONMENT=Production`
  - `ASPNETCORE_URLS=http://0.0.0.0:$PORT`
  - `MCP_MODE=false`
  - `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false`

**Railway Configuration Files:**
- `railway.json` - Main deployment configuration with Dockerfile builder
- `Dockerfile` - Multi-stage build using official Microsoft .NET 8 images
- `.dockerignore` - Optimized Docker build context
- `global.json` - .NET SDK version consistency

### Remote MCP Client Configuration

Since MCP clients expect stdio communication, use the bridge script to convert HTTP requests:

#### Step 1: Get the Bridge Script

Download `mcp-http-bridge.js` from this repository.

#### Step 2: Configure VS Code for Remote Access

Add to your `mcp.json`:

```json
{
  "mcpServers": {
    "wikipedia-remote": {
      "command": "node",
      "args": [
        "/path/to/your/mcp-http-bridge.js"
      ],
      "description": "Remote Wikipedia MCP Server on Render",
      "env": {
        "NODE_ENV": "production"
      }
    }
  }
}
```

#### Step 3: Test the Remote Connection

```bash
# Test the remote MCP server through the bridge
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{}}}' | node mcp-http-bridge.js
```

---

## 🔧 Environment Management

### .NET SDK Management

#### Required Version

This project requires .NET 8.0 SDK. Check your version:

```bash
dotnet --version
# Should output: 8.0.x
```

#### Install .NET 8.0

**macOS (Homebrew):**
```bash
brew install dotnet@8
```

**Windows:**
- Download from [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)

**Linux (Ubuntu/Debian):**
```bash
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0
```

#### Multiple .NET Versions

If you have multiple .NET versions, ensure the project uses .NET 8:

**Check global.json:**
```json
{
  "sdk": {
    "version": "8.0.0",
    "rollForward": "latestMajor"
  }
}
```

**Set environment variable:**
```bash
export DOTNET_ROOT=/usr/local/share/dotnet
export PATH=$DOTNET_ROOT:$PATH
```

### Environment Variables

Customize behavior with these environment variables:

```bash
# Set environment
export DOTNET_ENVIRONMENT=Development
export ASPNETCORE_ENVIRONMENT=Development

# Logging levels
export ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT=Information
export ASPNETCORE_LOGGING__LOGLEVEL__MICROSOFT=Warning

# For HTTP mode
export ASPNETCORE_URLS=http://localhost:5070

# For remote deployment
export ASPNETCORE_URLS=http://0.0.0.0:$PORT
```

### Development Configuration

#### appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Wikipedia": {
    "BaseUrl": "https://en.wikipedia.org",
    "Timeout": "00:00:30"
  }
}
```

#### VS Code Launch Configuration

Add to `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Launch HTTP Mode",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/WikipediaMcpServer/bin/Debug/net8.0/WikipediaMcpServer.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/WikipediaMcpServer",
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    {
      "name": "Launch stdio Mode",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/WikipediaMcpServer/bin/Debug/net8.0/WikipediaMcpServer.dll",
      "args": ["--mcp"],
      "cwd": "${workspaceFolder}/src/WikipediaMcpServer",
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "console": "integratedTerminal"
    }
  ]
}
```

---

## 🧪 Testing Your Setup

### Automated Testing

Run the comprehensive test suite:

```bash
# Run all tests (206 total)
dotnet test

# Run only stdio tests (8 tests)
dotnet test --filter "FullyQualifiedName~StdioTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Manual Testing

#### stdio Mode Testing

```bash
# Start server
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp

# In another terminal, test with JSON-RPC messages:
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{}}}' | \
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp
```

#### HTTP Mode Testing

```bash
# Start HTTP server
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj

# Test endpoints
curl http://localhost:5070/health
curl http://localhost:5070/info
```

### Using Postman Collections

Import the provided Postman collections:

**For Local Testing:**
- `WikipediaMcpServer-MCP-JsonRPC-Collection.json`
- `WikipediaMcpServer-Environment.postman_environment.json`

**For Remote Testing:**
- `WikipediaMcpServer-Remote-Collection.json`
- `WikipediaMcpServer-Remote-Environment.postman_environment.json`

---

## 🛠️ Available Tools

Once configured, the Wikipedia MCP server provides these tools:

### 1. wikipedia_search
Search Wikipedia for articles
- **Parameter**: `query` (string) - The search term
- **Example**: "Search Wikipedia for artificial intelligence"

### 2. wikipedia_sections
Get sections/outline of a Wikipedia page
- **Parameter**: `topic` (string) - The Wikipedia page title
- **Example**: "What sections are available for the Python article?"

### 3. wikipedia_section_content
Get content from a specific section
- **Parameters**:
  - `topic` (string) - The Wikipedia page title
  - `sectionTitle` (string) - The section to retrieve
- **Example**: "Get the History section from the Artificial Intelligence page"

---

## 🏗️ Development Mode

### Running Both Modes Simultaneously

You can run both stdio and HTTP modes for development:

#### Terminal 1: HTTP Mode (for testing)
```bash
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj
# Listens on http://localhost:5070
# Endpoints: /mcp/rpc, /mcp, /health, /info
```

#### Terminal 2: stdio Mode (for AI clients)
```bash
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp
# Reads from stdin, writes to stdout
# For VS Code/Claude Desktop integration
```

### Hot Reload for Development

```bash
dotnet watch run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj
```

### Swagger Documentation

When running in HTTP mode, access API documentation at:
```
http://localhost:5070/swagger
```

---

## 📊 Performance Comparison

| Metric | stdio Mode | HTTP Mode |
|--------|------------|-----------|
| Startup time | ~1-2s | ~2-3s |
| First request | Fast | Fast |
| Security | Local only | Network exposed |
| Best for | AI clients | Testing/Remote |

---

## 🔄 Architecture Overview

### Transport Detection

The server automatically detects which mode to run:

```csharp
var isStdioMode = args.Contains("--mcp");

if (isStdioMode)
{
    await RunStdioModeAsync();  // stdio transport
    return;
}

// Default: HTTP transport with ASP.NET Core
```

### Code Reuse

Both modes share the same Wikipedia service logic:
- **stdio mode**: Uses direct JSON-RPC handlers
- **HTTP mode**: Uses ASP.NET Core controllers
- **Same business logic**: Both call `WikipediaService` methods

---

## ✅ Setup Verification Checklist

- [ ] .NET 8.0 SDK installed and working
- [ ] Project builds without errors: `dotnet build`
- [ ] Tests pass: `dotnet test`
- [ ] stdio mode starts: `dotnet run ... -- --mcp`
- [ ] HTTP mode starts: `dotnet run ...`
- [ ] VS Code MCP configuration added
- [ ] VS Code MCP connection indicator shows green
- [ ] Can execute Wikipedia tools in VS Code
- [ ] Health check responds: `curl http://localhost:5070/health`

---

## 🆘 Common Issues

### Build Errors

**Issue**: `SDK 'Microsoft.NET.Sdk.Web' not found`
**Solution**: Install .NET 8.0 SDK

**Issue**: Package restore fails
**Solution**: Clear NuGet cache
```bash
dotnet nuget locals all --clear
dotnet restore
```

### VS Code Integration Issues

**Issue**: MCP server not connecting
**Solutions**:
1. Check absolute paths in `mcp.json`
2. Verify `--mcp` flag in args array
3. Reload VS Code window
4. Check VS Code output panel for errors

### Network Issues

**Issue**: Wikipedia API timeouts
**Solution**: Check internet connection and Wikipedia API status

**Issue**: Port 5070 already in use
**Solution**: Kill existing processes or change port
```bash
lsof -ti:5070 | xargs kill -9
```

---

This setup guide covers all scenarios for using the Wikipedia MCP Server effectively. Choose the approach that best fits your development workflow!