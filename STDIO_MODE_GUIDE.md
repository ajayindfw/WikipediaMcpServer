# Wikipedia MCP Server - stdio Mode Guide

## Overview

The Wikipedia MCP Server now supports **dual transport modes**:

1. **stdio mode** - For local development and AI client integration (VS Code, Claude Desktop)
2. **HTTP mode** - For remote deployments and Postman testing

## stdio Mode Features

### What is stdio Mode?

stdio (standard input/output) mode enables the MCP server to communicate via:
- **stdin** - Reads JSON-RPC 2.0 requests from standard input
- **stdout** - Writes JSON-RPC 2.0 responses to standard output
- **stderr** - Logs diagnostic messages without interfering with protocol

This enables seamless integration with AI clients like:
- ✅ VS Code MCP Extension
- ✅ Claude Desktop
- ✅ Other MCP-compatible clients

### Why stdio Mode?

MCP clients like VS Code and Claude Desktop prefer stdio transport because:
- 🔐 **Secure** - No network ports exposed
- 🚀 **Fast** - Direct process communication
- 🎯 **Simple** - No HTTP/SSE overhead
- 🔧 **Debuggable** - Clean separation of logs (stderr) and protocol (stdout)

## Usage

### Starting in stdio Mode

Add the `--mcp` flag to run in stdio mode:

```bash
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp
```

### Starting in HTTP Mode (Default)

Run without the `--mcp` flag for HTTP mode:

```bash
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj
```

The server will start on http://localhost:5070

## VS Code Integration

### mcp.json Configuration

Add this to your VS Code `mcp.json`:

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

### VS Code Settings

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

## Claude Desktop Integration

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

## Testing stdio Mode

### Automated Testing

Run the comprehensive automated stdio test suite:

```bash
# Run all stdio tests (8 tests)
dotnet test --filter "FullyQualifiedName~StdioTests"
```

This tests:
- ✅ MCP initialize handshake
- ✅ Tools list discovery
- ✅ Wikipedia search tool execution
- ✅ Wikipedia sections retrieval
- ✅ Wikipedia section content access
- ✅ Error handling (invalid methods, malformed JSON, missing parameters)

### Manual JSON-RPC Testing

You can pipe JSON-RPC messages directly:

```bash
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{}}}' | \
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp
```

## Protocol Implementation

### Supported JSON-RPC Methods

| Method | Description | Status |
|--------|-------------|--------|
| `initialize` | MCP handshake | ✅ |
| `tools/list` | List available Wikipedia tools | ✅ |
| `tools/call` | Execute Wikipedia tool | ✅ |

### Available Tools

1. **wikipedia_search**
   - Search Wikipedia for topics and articles
   - Input: `{ "query": "search term" }`

2. **wikipedia_sections**
   - Get page outline and sections
   - Input: `{ "topic": "article title" }`

3. **wikipedia_section_content**
   - Get specific section content
   - Input: `{ "topic": "article title", "sectionTitle": "section name" }`

## Logging and Debugging

### Log Channels

- **stderr** - Diagnostic logs (safe for debugging)
  - Server startup messages
  - Request/response traces
  - Error messages
  
- **stdout** - JSON-RPC protocol only (DO NOT LOG HERE)
  - Clean JSON-RPC responses
  - No debug messages

### Example stderr Output

```
🔧 Starting Wikipedia MCP Server in stdio mode...
📡 Reading JSON-RPC messages from stdin, writing to stdout
✅ stdio mode initialized - ready for JSON-RPC messages
📥 Received: {"jsonrpc":"2.0","id":1,"method":"initialize"...
🎯 Method: initialize
📤 Sent response
```

## Architecture

### Transport Detection

The server detects which mode to run based on command-line arguments:

```csharp
var isStdioMode = args.Contains("--mcp");

if (isStdioMode)
{
    await RunStdioModeAsync();  // stdio transport
    return;
}

// Default: HTTP transport with ASP.NET Core
```

### stdio Mode Implementation

```csharp
static async Task RunStdioModeAsync()
{
    // 1. Setup DI container with Wikipedia service
    // 2. Read JSON-RPC from Console.In (stdin)
    // 3. Parse and route to appropriate handler
    // 4. Execute Wikipedia tool
    // 5. Write JSON-RPC response to Console.Out (stdout)
}
```

### HTTP Mode Implementation

Uses ASP.NET Core with ModelContextProtocol.AspNetCore package:
- Kestrel web server on port 5070
- Server-Sent Events (SSE) for streaming
- HTTP POST endpoints for JSON-RPC

## Troubleshooting

### Server starts in HTTP mode when it should use stdio

**Problem**: Server shows web server startup messages instead of stdio mode

**Solution**: Ensure you're passing the `--mcp` flag correctly:
```bash
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp
#                                                                      ^^^^ Note the -- separator
```

### VS Code can't connect to server

**Problem**: VS Code MCP extension can't communicate

**Checklist**:
1. ✅ Is `--mcp` flag in args array?
2. ✅ Is project path absolute or uses `${workspaceFolder}`?
3. ✅ Can you run the server manually in stdio mode?
4. ✅ Check VS Code output panel for errors

### No response from tools

**Problem**: Tools execute but no results returned

**Debug Steps**:
1. Run automated tests: `dotnet test --filter "FullyQualifiedName~StdioTests"`
2. Check stderr for error messages
3. Verify Wikipedia API is accessible
4. Test with simple query: `python programming`

## Performance

### stdio vs HTTP Mode

| Metric | stdio | HTTP |
|--------|-------|------|
| Startup time | ~1-2s | ~2-3s |
| First request | Fast | Fast |
| Throughput | High | High |
| Latency | Low (direct) | Low (localhost) |
| Security | Local only | Network exposed |

### Recommendations

- **Development**: Use stdio mode for faster iteration
- **Testing**: Use HTTP mode with Postman
- **Production**: Use HTTP mode with proper hosting
- **AI Clients**: Always use stdio mode

## References

- [Model Context Protocol Specification](https://spec.modelcontextprotocol.io/)
- [JSON-RPC 2.0 Specification](https://www.jsonrpc.org/specification)
- [Microsoft MCP SDK Documentation](https://github.com/microsoft/mcp-dotnet)

## Version History

- **v8.0.0** - Added stdio mode support for VS Code and Claude Desktop
- **v7.0.0** - Microsoft MCP SDK migration with HTTP-only support
- **v6.0.0** - Initial McpFramework.NET implementation

---

✅ **stdio mode is ready for VS Code and Claude Desktop integration!**
