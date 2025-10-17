# 🔄 Postman Collection Migration Guide

## Overview

The Wikipedia MCP Server has been **migrated from custom REST API to Microsoft ModelContextProtocol SDK v0.4.0-preview.2** using **JSON-RPC 2.0 protocol**. This requires completely new Postman collections.

## 🚨 Breaking Changes

### Before (DEPRECATED - Now Removed)
- **File**: `WikipediaMcpServer-Postman-Collection.DEPRECATED.json` *(deleted)*
- **Protocol**: Traditional REST API
- **Endpoints**: 
  - `GET /api/wikipedia/search?query=...`
  - `GET /api/wikipedia/sections?topic=...`
  - `GET /api/wikipedia/section-content?topic=...&sectionTitle=...`
- **Status**: ❌ **NO LONGER EXISTS**

### After (CURRENT)
- **File**: `WikipediaMcpServer-MCP-JsonRPC-Collection.json`
- **Protocol**: JSON-RPC 2.0 over HTTP (MCP Standard)
- **Endpoint**: `POST /` (Single endpoint for all operations)
- **Format**: JSON-RPC 2.0 messages
- **Status**: ✅ **CURRENT AND WORKING**

## 📋 New Collection Features

### Available Requests
1. **Health & Info**
   - `GET /health` - Server health check
   - `GET /info` - Server information

2. **MCP Protocol**
   - `POST /` - `initialize` method (MCP handshake)
   - `POST /` - `tools/list` method (List available tools)

3. **Wikipedia Tools** (via `tools/call`)
   - `wikipedia_search` - Search Wikipedia articles
   - `wikipedia_sections` - Get page sections
   - `wikipedia_section_content` - Get section content

### Example JSON-RPC Requests

#### Initialize MCP
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "protocolVersion": "2024-11-05",
    "capabilities": {}
  }
}
```

#### Search Wikipedia
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "wikipedia_search",
    "arguments": {
      "query": "Artificial intelligence"
    }
  }
}
```

## 🔧 Migration Steps

### For Postman Users

1. **Import new collection**: `WikipediaMcpServer-MCP-JsonRPC-Collection.json`
2. **Keep environment**: `WikipediaMcpServer-Environment.postman_environment.json` (still compatible)
3. **Test the new requests**: All requests now use JSON-RPC 2.0 format

### For API Users

Replace REST API calls:

**Old (❌ Broken)**:
```bash
curl "http://localhost:5070/api/wikipedia/search?query=Python"
```

**New (✅ Working)**:
```bash
curl -X POST "http://localhost:5070/" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"wikipedia_search","arguments":{"query":"Python"}}}'
```

## 📊 Response Format Changes

### Old REST Response
```json
{
  "title": "Python (programming language)",
  "summary": "Python is a high-level...",
  "url": "https://en.wikipedia.org/wiki/Python_(programming_language)"
}
```

### New JSON-RPC Response (via SSE)
```
event: message
data: {"result":{"content":[{"type":"text","text":"Wikipedia search result for 'Python':\n\n**Python (programming language)**\nURL: https://en.wikipedia.org/wiki/Python_(programming_language)\nSummary: Python is a high-level..."}]},"id":1,"jsonrpc":"2.0"}
```

## ✅ Benefits of New Architecture

1. **Standards Compliance**: Full JSON-RPC 2.0 and MCP protocol compliance
2. **Microsoft SDK**: Using official Microsoft ModelContextProtocol SDK
3. **Better Integration**: Works seamlessly with Claude Desktop and other MCP clients
4. **Enhanced Logging**: Detailed request/response logging for debugging
5. **Server-Sent Events**: Real-time streaming responses via SSE

## 🧪 Testing

The new collection includes:
- ✅ 15+ JSON-RPC 2.0 test requests  
- ✅ 40+ automated test assertions
- ✅ MCP protocol compliance validation
- ✅ Tool discovery and invocation testing
- ✅ Error handling validation
- ✅ Performance testing

## 📚 Additional Resources

- **README.md** - Updated with new JSON-RPC examples
- **Automated Tests** - `dotnet test` runs 206 comprehensive tests
- **Microsoft MCP SDK Documentation** - Official SDK reference

---

**Migration Date**: After Microsoft MCP SDK integration (v8.0+)
**SDK Version**: Microsoft ModelContextProtocol v0.4.0-preview.2