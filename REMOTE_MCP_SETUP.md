# Remote MCP Setup Guide

## Overview

The Wikipedia MCP Server now supports **BOTH local (stdio) and remote (HTTP) MCP access** simultaneously!

## 🎯 Supported Transport Methods

### 1. **stdio Mode** (Local)
- **Use Case**: Local VS Code, Claude Desktop integration
- **How to Run**: `dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp`
- **Protocol**: JSON-RPC over stdin/stdout
- **Status**: ✅ Working perfectly

### 2. **HTTP RPC Mode** (Remote)
- **Use Case**: Remote access from any MCP client
- **Endpoint**: `POST /mcp/rpc`
- **Protocol**: JSON-RPC over HTTP POST
- **Status**: ✅ Newly implemented in v8.2

### 3. **HTTP SDK Mode** (Advanced)
- **Use Case**: Microsoft MCP SDK clients
- **Endpoint**: `POST /mcp`
- **Protocol**: SSE/WebSocket via Microsoft MCP SDK
- **Status**: ✅ Available (Microsoft SDK default)

---

## 🚀 Quick Start - Remote Access

### Step 1: Server is Already Deployed
The Wikipedia MCP Server is running on Render at:
```
https://wikipediamcpserver.onrender.com
```

### Step 2: Configure VS Code MCP

Edit your `~/.config/Code/User/mcp.json` (macOS) or equivalent:

```json
{
  "servers": {
    "wikipedia-remote": {
      "command": "node",
      "args": [
        "/path/to/WikipediaMcpServer/mcp-http-bridge.js"
      ],
      "description": "Remote Wikipedia MCP Server on Render",
      "env": {
        "NODE_ENV": "production"
      }
    }
  }
}
```

### Step 3: Reload VS Code
- Press `Cmd+Shift+P` (macOS) or `Ctrl+Shift+P` (Windows/Linux)
- Run: "Developer: Reload Window"

### Step 4: Use Wikipedia Tools
The remote MCP server will now appear in your VS Code MCP extension with 3 tools:
- `wikipedia_search` - Search Wikipedia
- `wikipedia_sections` - Get page sections
- `wikipedia_section_content` - Get section content

---

## 📡 Technical Architecture

### How Remote MCP Works

```
┌─────────────────────┐
│   VS Code Client    │
│   (stdio mode)      │
└──────────┬──────────┘
           │ stdin/stdout (JSON-RPC)
           ↓
┌─────────────────────┐
│ mcp-http-bridge.js  │  ← Runs locally
│  (Node.js script)   │
└──────────┬──────────┘
           │ HTTP POST (JSON-RPC)
           ↓
┌─────────────────────┐
│  Render Server      │
│  /mcp/rpc endpoint  │  ← Deployed remotely
│  (C# .NET API)      │
└──────────┬──────────┘
           │
           ↓
┌─────────────────────┐
│ Wikipedia API       │
│ (REST v1)           │
└─────────────────────┘
```

### Components

1. **VS Code MCP Extension**
   - Expects stdio communication
   - Sends JSON-RPC messages to bridge

2. **mcp-http-bridge.js**
   - Converts stdio → HTTP POST
   - Forwards to remote server
   - Returns responses via stdout

3. **Server `/mcp/rpc` Endpoint**
   - Accepts JSON-RPC over HTTP POST
   - Processes: `initialize`, `tools/list`, `tools/call`
   - Returns JSON-RPC responses

4. **WikipediaService**
   - Calls Wikipedia REST API v1
   - Returns formatted results

---

## 🧪 Testing

### Test Remote Endpoint Directly

```bash
# Initialize
curl -X POST https://wikipediamcpserver.onrender.com/mcp/rpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'

# List tools
curl -X POST https://wikipediamcpserver.onrender.com/mcp/rpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}'

# Search Wikipedia
curl -X POST https://wikipediamcpserver.onrender.com/mcp/rpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"wikipedia_search","arguments":{"query":"Artificial Intelligence"}}}'
```

### Test Local Bridge

```bash
# Test with local server
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' | \
  REMOTE_SERVER_URL=http://localhost:5070/mcp/rpc \
  MCP_DEBUG=true \
  node mcp-http-bridge.js

# Test with remote server (default)
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' | \
  MCP_DEBUG=true \
  node mcp-http-bridge.js
```

---

## 🔧 Development

### Running Both Modes Simultaneously

#### Terminal 1: HTTP Mode (for remote access)
```bash
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj
# Listens on http://localhost:5070
# Endpoints: /mcp/rpc, /mcp, /health, /info
```

#### Terminal 2: stdio Mode (for local access)
```bash
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp
# Reads from stdin, writes to stdout
# For VS Code/Claude Desktop local integration
```

Both modes use the **same codebase** and **same Wikipedia service**!

---

## 📋 Code Implementation

### Program.cs Structure

```csharp
// Main entry point - checks for --mcp flag
if (isStdioMode)
{
    await RunStdioModeAsync();  // stdio handlers
    return;
}

// HTTP mode setup
var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<WikipediaTools>();

var app = builder.Build();

// Configure endpoints
app.MapMcp();              // Microsoft SDK endpoint (/mcp)
app.MapPost("/mcp/rpc", …); // Custom HTTP RPC endpoint
app.MapHealthChecks("/health");
app.MapGet("/info", …);

app.Run();
```

### Handler Methods

Both stdio and HTTP modes share the same logic:

- **stdio mode**: `HandleInitialize()`, `HandleToolsList()`, `HandleToolsCall()`
  - Returns: Compact JSON strings for stdout
  
- **HTTP mode**: `HandleInitializeHttp()`, `HandleToolsListHttp()`, `HandleToolsCallHttp()`
  - Returns: C# objects (auto-serialized by ASP.NET Core)

---

## 🌐 Deployment

### Render Configuration

The server is configured in `render.yaml`:

```yaml
services:
  - type: web
    runtime: docker
    healthCheckPath: /health
    startCommand: dotnet ./publish/WikipediaMcpServer.dll
    envVars:
      - key: ASPNETCORE_ENVIRONMENT
        value: Production
      - key: MCP_MODE
        value: "false"  # HTTP mode for remote access
```

### Docker Support

Both `Dockerfile` and `docker-compose.yml` are configured for HTTP mode by default.

To run in stdio mode in Docker, add:
```dockerfile
CMD ["dotnet", "WikipediaMcpServer.dll", "--mcp"]
```

---

## 📊 Endpoints Summary

| Endpoint | Method | Purpose | Transport |
|----------|--------|---------|-----------|
| `/mcp/rpc` | POST | **Remote MCP (JSON-RPC over HTTP)** | HTTP |
| `/mcp` | POST | Microsoft SDK (SSE/WebSocket) | HTTP |
| `/health` | GET | Health check | HTTP |
| `/info` | GET | Server information | HTTP |
| `/swagger` | GET | API documentation | HTTP |
| `--mcp flag` | stdin/stdout | **Local MCP (stdio)** | stdio |

---

## ✅ Advantages of This Architecture

1. **🎯 Dual Transport**: Supports both local (stdio) and remote (HTTP) simultaneously
2. **🔄 Code Reuse**: Same handler logic for both modes
3. **📦 Simple Protocol**: Standard JSON-RPC 2.0 over HTTP POST
4. **🚀 Production Ready**: Deployed on Render with health checks
5. **🧪 Easy Testing**: Can test with curl or Postman
6. **🔌 Universal**: Works with any MCP client that supports stdio
7. **🌐 Remote Access**: Team members can use without local deployment

---

## 🐛 Troubleshooting

### Issue: Bridge fails with "Internal error -32603"
**Solution**: Ensure you're using the latest code with `/mcp/rpc` endpoint

### Issue: No response from bridge
**Solution**: Check server is running and accessible:
```bash
curl https://wikipediamcpserver.onrender.com/health
```

### Issue: Render server not picking up changes
**Solution**: Trigger manual deploy in Render dashboard

### Issue: VS Code MCP not showing tools
**Solution**: 
1. Check `mcp.json` path is correct
2. Reload VS Code window
3. Check MCP extension logs

---

## 📚 Related Documentation

- [README.md](README.md) - Main project documentation
- [TESTING_GUIDE.md](TESTING_GUIDE.md) - Comprehensive testing guide
- [MCP_SETUP_GUIDE.md](MCP_SETUP_GUIDE.md) - Local stdio setup
- [render.yaml](render.yaml) - Deployment configuration

---

## 🎉 Version History

- **v8.2** (Current): Added `/mcp/rpc` endpoint for remote HTTP access
- **v8.1**: Fixed stdio JSON-RPC formatting, added parameter compatibility
- **v8.0**: Migrated to Microsoft MCP SDK
- **v7.0**: Custom MCP implementation

---

## 🤝 Contributing

To add new tools that work in both modes:

1. Add tool definition to `WikipediaTools.cs`
2. Update both `HandleToolsList()` and `HandleToolsListHttp()`
3. Update both `HandleToolsCall()` and `HandleToolsCallHttp()`
4. Test in both stdio and HTTP modes
5. Update documentation

The architecture ensures any new tool automatically works in both local and remote modes!
