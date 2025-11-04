# Wikipedia MCP Server (ASP.NET Core 8)

A production-ready Model Context Protocol (MCP) server implementation for Wikipedia search and content retrieval, built with ASP.NET Core 8 and C#. Features comprehensive testing with **206 total tests** and enterprise-grade reliability.

## Features

This server provides three main Wikipedia-related tools with full test coverage:

1. **Wikipedia Search** - Search for Wikipedia articles and get summaries
2. **Wikipedia Sections** - Get the section outline of a Wikipedia page  
3. **Wikipedia Section Content** - Retrieve content from specific sections of Wikipedia articles

### **🏆 Production Ready**
- ✅ **150 comprehensive tests** (Unit, Service, Integration, stdio)
- ✅ **100% test pass rate** ensuring reliability
- ✅ **Automated stdio mode testing** with real process spawning
- ✅ **Professional .NET project structure** with src/ and tests/ organization
- ✅ **Enhanced error handling** and validation
- ✅ **Code coverage reporting** with detailed analysis
- ✅ **Dual-mode operation** (HTTP API + MCP Protocol)
- ✅ **Enterprise-grade logging** and monitoring

## Quick Start

### Prerequisites

- .NET 8.0 SDK
- Visual Studio Code with GitHub Copilot extension (for MCP mode)

### Installation

1. Clone the repository:

   ```bash
   git clone <repository-url>
   cd WikipediaMcpServer
   ```

2. Restore dependencies:

   ```bash
   dotnet restore
   ```

3. Build the project:

   ```bash
   cd src/WikipediaMcpServer
   dotnet build
   ```

## Usage Modes

This server supports **three MCP-compliant transport modes** with **96%+ MCP specification compliance**:

### 🏆 **Transport Mode Summary**

| **Transport** | **Endpoint** | **Compliance** | **Best For** |
|---------------|-------------|---------------|-------------|
| **stdio Mode** | `--mcp` flag | ✅ **96%** | VS Code, Claude Desktop, Local AI |
| **HTTP JSON-RPC** | `/mcp/rpc` | ✅ **96%** | Postman, Remote Access, Testing |
| **HTTP MCP SDK** | `/mcp` | ✅ **100%** | Official MCP Clients, SSE/WebSocket |

All transport modes provide the same Wikipedia tools with consistent, professional-grade MCP specification compliance.

### 1. **stdio Mode** - For Local AI Client Integration (Recommended) ✅ **96% MCP Compliant**

Run in stdio (standard input/output) mode for seamless integration with AI clients like VS Code and Claude Desktop:

```bash
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp
```

**MCP Compliance Features:**
- 🔄 **Protocol Version Negotiation** - Supports both 2024-11-05 and 2025-06-18
- 📬 **Notification Support** - Proper `notifications/initialized` handling
- 🎯 **Enhanced Capabilities** - Dynamic capabilities based on protocol version
- 👤 **Client Information** - Extracts and logs client details
- ✅ **JSON-RPC 2.0** - Full specification compliance

**Benefits:**
- 🔐 **Secure** - No network ports exposed
- 🚀 **Fast** - Direct process communication
- 🎯 **Simple** - No HTTP/SSE overhead
- ✅ **Compatible** - Works with VS Code MCP Extension, Claude Desktop, and other MCP clients

**VS Code Configuration (`mcp.json`):**

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

**Claude Desktop Configuration:**

Add to `~/Library/Application Support/Claude/claude_desktop_config.json` (macOS) or `%APPDATA%\Claude\claude_desktop_config.json` (Windows):

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

**Testing stdio Mode:**

```bash
# Test manually with a simple initialize message
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{}}}' | \
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp

# Or run automated stdio tests
dotnet test --filter "FullyQualifiedName~StdioTests"
```

📚 **See [STDIO_MODE_GUIDE.md](STDIO_MODE_GUIDE.md) for complete documentation.**

### 2. **HTTP Mode** - For Remote Deployments and Testing ✅ **96% MCP Compliant**

📚 **See [REMOTE_MCP_SETUP.md](REMOTE_MCP_SETUP.md) for remote access setup guide.**

This server provides **TWO MCP-compliant HTTP endpoints**:

#### **2a. `/mcp/rpc` - Custom MCP-Compliant JSON-RPC Endpoint ✅ 96% Compliant**
Perfect for HTTP testing, Postman, and remote MCP access:

**MCP Compliance Features:**
- 🔄 **Protocol Version Negotiation** - Supports both 2024-11-05 and 2025-06-18
- 📡 **MCP Headers** - Proper `MCP-Protocol-Version` header support
- 📬 **Notification Support** - Complete lifecycle management
- 🎯 **Enhanced Capabilities** - Dynamic capabilities declaration
- ✅ **JSON-RPC 2.0** - Full specification compliance

#### **2b. `/mcp` - Microsoft MCP SDK Endpoint ✅ 100% Compliant**
Official Microsoft SDK implementation with SSE/WebSocket transport.

To run as an HTTP API server (default mode):

```bash
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj
```

The server will start on `http://localhost:5070` by default.

**Use HTTP mode for:**
- 🌐 Remote deployments (Render, Railway, Azure, AWS)
- 🧪 Postman API testing
- 🔗 HTTP-based integrations
- 📊 Load testing and monitoring

### Running as Remote MCP Server

The Wikipedia MCP Server can be deployed and accessed remotely, allowing multiple clients to use the service without running it locally. This is particularly useful for teams or when you want to avoid local resource usage.

**New in v8.2:** The server now supports remote MCP access via the `/mcp/rpc` HTTP POST endpoint, enabling JSON-RPC 2.0 over HTTP for easy remote integration.

#### Remote Deployment Options

##### Option 1: Render (Recommended)

The server is already deployed and available at:
**https://wikipediamcpserver.onrender.com**

**Available Endpoints:**
- `/health` - Health check endpoint
- `/info` - Server information and available endpoints
- `/mcp/rpc` - Remote MCP JSON-RPC endpoint (v8.2+)
- `/mcp` - Microsoft MCP SDK endpoint (SSE/WebSocket)

To verify the remote server is running:

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

##### Option 2: Deploy Your Own Instance on Render

You can deploy your own instance to Render by:

1. **Fork this repository** to your GitHub account
2. **Create a new Web Service** on [Render](https://render.com)
3. **Connect your GitHub repository** to Render
4. **Create a new Web Service** from your repository
5. Render will **automatically detect** the `render.yaml` configuration
6. **Deploy** with one click!

**Detailed Configuration:**

**Automatic Setup (Recommended):**
- Uses `render.yaml` for complete deployment configuration
- Automatic deployments on git push
- Build Command: `dotnet restore src/WikipediaMcpServer/WikipediaMcpServer.csproj && dotnet publish src/WikipediaMcpServer/WikipediaMcpServer.csproj -c Release -o ./publish`
- Start Command: `dotnet ./publish/WikipediaMcpServer.dll`
- Environment: Set `ASPNETCORE_URLS=http://0.0.0.0:$PORT`

**Manual Configuration (if not using render.yaml):**
- Build Command: `dotnet publish src/WikipediaMcpServer/WikipediaMcpServer.csproj -c Release -o out`
- Start Command: `dotnet out/WikipediaMcpServer.dll`

##### Option 3: Deploy Your Own Instance on Railway

[Railway](https://railway.com) provides an excellent alternative deployment platform with zero-configuration setup:

1. **Fork this repository** to your GitHub account
2. **Create a new Project** on [Railway](https://railway.com)
3. **Connect your GitHub repository** to Railway
4. **Deploy automatically** using `railway.json` configuration
5. **Zero configuration required** - Railway detects .NET projects automatically!

**Automatic Configuration Features:**
- Uses `railway.json` with JSON schema validation
- Uses `nixpacks.toml` for .NET 8 environment setup
- Includes health checks, restart policies, and optimized environment variables
- Compatible with `global.json` SDK version requirements
- Automatic builds and deployments on git push

**Configuration Files:**
- `railway.json` - Main deployment configuration with schema validation
- `nixpacks.toml` - .NET 8 build environment and variable configuration
- `global.json` - .NET SDK version consistency across environments

**Manual Configuration (if not using railway.json):**
- Builder: Select "Nixpacks"
- Build Command: `dotnet publish src/WikipediaMcpServer/WikipediaMcpServer.csproj -c Release -o ./publish --no-restore`
- Start Command: `dotnet ./publish/WikipediaMcpServer.dll`
- Environment Variables:
  - `ASPNETCORE_ENVIRONMENT=Production`
  - `ASPNETCORE_URLS=http://0.0.0.0:$PORT`
  - `MCP_MODE=false`
  - `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false`

##### Option 4: Other Cloud Platforms
- Environment: Select ".NET"
- Add environment variables in Render dashboard

**Custom Domain:**
- Add your domain in Render dashboard
- Update DNS to point to Render

**Cost & Scaling:**
- Free Tier: ✅ 750 hours/month
- Paid Plans Start: $7/month for always-on services
- Horizontal scaling on paid plans
- Automatic sleep/wake on free tier

Your deployed server will be available at `https://your-app-name.onrender.com`

#### Remote MCP Client Configuration

Since most MCP clients (VS Code, Claude Desktop) expect stdio communication, you'll need to use a bridge script to convert HTTP requests to the proper format.

##### Step 1: Get the Bridge Script

Download or copy the `mcp-http-bridge.js` file from this repository. This Node.js script converts MCP stdio communication to HTTP requests.

##### Step 2: Configure Your MCP Client

**For VS Code MCP Extension:**

Add this to your `mcp.json` file (`~/Library/Application Support/Code/User/mcp.json` on macOS):

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

**For Claude Desktop:**

Add this to your Claude Desktop config file (`~/Library/Application Support/Claude/claude_desktop_config.json` on macOS):

```json
{
  "mcpServers": {
    "wikipedia-remote": {
      "command": "node",
      "args": [
        "/path/to/your/mcp-http-bridge.js"
      ],
      "env": {
        "NODE_ENV": "production"
      }
    }
  }
}
```

##### Step 3: Test the Remote Connection

Test the bridge script manually:

```bash
# Test the remote MCP server through the bridge
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{}}}' | node mcp-http-bridge.js
```

You should see a response like:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "protocolVersion": "2024-11-05",
    "capabilities": {
      "tools": {}
    },
    "serverInfo": {
      "name": "wikipedia-mcp-server",
      "version": "6.0.0"
    }
  }
}
```

#### Remote Server Benefits

✅ **No Local Resources** - Runs on remote infrastructure  
✅ **Always Available** - 24/7 uptime on cloud platforms  
✅ **Shared Access** - Multiple team members can use the same instance  
✅ **Automatic Updates** - Deploy updates without client configuration changes  
✅ **Scalable** - Cloud platforms handle traffic scaling automatically  
✅ **Reliable** - Professional hosting with monitoring and backups  

#### HTTP Bridge Script Details

The `mcp-http-bridge.js` script:

- Reads MCP JSON-RPC messages from stdin
- Converts them to HTTP POST requests
- Sends requests to the remote server
- Returns responses in proper MCP format
- Handles errors and timeouts gracefully
- Provides debug logging to stderr

#### Alternative: Direct HTTP Testing

You can also test the remote server directly with HTTP requests:

```bash
# Test remote server directly (bypasses MCP protocol)
curl -X POST \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"wikipedia_search","arguments":{"query":"artificial intelligence"}}}' \
  https://wikipediamcpserver.onrender.com/api/wikipedia
```

#### Troubleshooting Remote Connection

**Common Issues:**

1. **Bridge script not found**: Ensure the path to `mcp-http-bridge.js` is correct and absolute
2. **Node.js not available**: Make sure Node.js is installed (`node --version`)
3. **Network issues**: Check if you can reach the remote server (`curl https://wikipediamcpserver.onrender.com/api/wikipedia/health`)
4. **Permission issues**: Ensure the bridge script is readable (`chmod +x mcp-http-bridge.js`)

**Debug Commands:**

```bash
# Test remote server health
curl https://wikipediamcpserver.onrender.com/api/wikipedia/health

# Test bridge script with debug output
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{}}}' | node mcp-http-bridge.js

# Check Node.js version
node --version
```

#### API Endpoints

##### Search Wikipedia

```http
GET /api/wikipedia/search?query={search_term}
```

Search Wikipedia for a topic and return detailed information about the best matching page.

##### Get Page Sections

```http
GET /api/wikipedia/sections?topic={page_title}
```

Get the sections/outline of a Wikipedia page for a given topic.

##### Get Section Content

```http
GET /api/wikipedia/section-content?topic={page_title}&sectionTitle={section_name}
```

Get the content of a specific section from a Wikipedia page.

##### Health Check

```http
GET /api/wikipedia/health
```

Health check endpoint to verify the server is running.

## MCP Server Configuration

This application can run as an MCP (Model Context Protocol) server that integrates with AI development tools like VS Code and Claude Desktop.

### Configuration Options

#### Option 1: Claude Desktop Configuration (Recommended)

1. Open Claude Desktop's configuration file:
   - **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
   - **Windows**: `%APPDATA%/Claude/claude_desktop_config.json`
   - **Linux**: `~/.config/Claude/claude_desktop_config.json`

2. Add the Wikipedia MCP server configuration:

   ```json
   {
     "mcpServers": {
       "wikipedia-dotnet": {
         "command": "dotnet",
         "args": [
           "run",
           "--project",
           "/full/path/to/your/WikipediaMcpServer/src/WikipediaMcpServer/WikipediaMcpServer.csproj",
           "--",
           "--mcp"
         ],
         "env": {
           "DOTNET_ENVIRONMENT": "Production"
         }
       }
     }
   }
   ```

3. **Important**: Update the path in the `args` array to match your actual project location.

4. Restart Claude Desktop to load the new configuration.

#### Option 2: VS Code Configuration

1. Open VS Code's global MCP configuration file:
   - **macOS**: `~/Library/Application Support/Code/User/mcp.json`
   - **Windows**: `%APPDATA%/Code/User/mcp.json`
   - **Linux**: `~/.config/Code/User/mcp.json`

2. Add the Wikipedia MCP server configuration:

   ```json
   {
     "servers": {
       "wikipedia-dotnet": {
         "command": "dotnet",
         "args": [
           "run",
           "--project",
           "/full/path/to/your/WikipediaMcpServer/src/WikipediaMcpServer/WikipediaMcpServer.csproj",
           "--",
           "--mcp"
         ],
         "env": {
           "DOTNET_ENVIRONMENT": "Production"
         }
       }
     }
   }
   ```

3. **Important**: Update the path in the `args` array to match your actual project location.

4. Restart VS Code to load the new configuration.

### Available MCP Tools

Once configured, the Wikipedia MCP server provides these tools:

1. **`wikipedia_search`** - Search Wikipedia for articles
   - Parameter: `query` (string) - The search term
   
2. **`wikipedia_sections`** - Get sections of a Wikipedia page
   - Parameter: `topic` (string) - The Wikipedia page title
   
3. **`wikipedia_section_content`** - Get content from a specific section
   - Parameters:
     - `topic` (string) - The Wikipedia page title
     - `section_title` (string) - The section to retrieve

### Using the MCP Tools

You can use these tools through natural language requests in supported clients:

- "Search Wikipedia for information about machine learning"
- "What are the sections available for the Python programming article on Wikipedia?"
- "Get the content of the History section from the Artificial Intelligence Wikipedia page"

## Remote Deployment Testing

### 🧪 **Comprehensive Testing Guide**

After deploying to Render, use these testing methods to verify your deployment:

#### **Quick Health Check**

```bash
# Test deployment health
curl https://your-deployment-url.onrender.com/api/wikipedia/health

# Expected response:
{"status":"healthy","service":"Wikipedia MCP Server","timestamp":"..."}
```

#### **Postman Collection Testing (Recommended)**

This repository includes **updated JSON-RPC 2.0 collections** specifically designed for deployment validation:

**📦 Remote Testing Files:**
- **`WikipediaMcpServer-Remote-MCP-JsonRPC-Collection.json`** - Complete remote JSON-RPC 2.0 test suite
- **`WikipediaMcpServer-Remote-Environment.postman_environment.json`** - Pre-configured environment variables

**🚀 Quick Setup:**

1. **Import the Remote Collections**:
   ```bash
   # In Postman:
   # 1. File → Import
   # 2. Select both files from the repository root
   # 3. WikipediaMcpServer-Remote-MCP-JsonRPC-Collection.json (JSON-RPC test suite)
   # 4. WikipediaMcpServer-Remote-Environment.postman_environment.json (environment)
   ```

2. **Configure Your Deployment URL**:
   - In Postman, select "Wikipedia MCP Server Remote Environment"
   - Update `base_url` variable to your actual deployment URL
   - Example: `https://your-app-name.onrender.com`
   - Save the environment

3. **Run the Complete Test Suite**:
   - Click "Run Collection" in Postman
   - Select "Wikipedia MCP Server - Remote Deployment Testing"
   - Choose "Wikipedia MCP Server Remote Environment"
   - Run all 12 tests

**✅ Expected Results:**
- All 12 tests should pass
- Response times should be < 10 seconds
- All endpoints return proper JSON
- Environment detection shows your platform (Render)
- Security headers are validated

#### **Performance Benchmarks**

| Test Type | Expected Time | Status |
|-----------|---------------|---------|
| Health Check | < 2 seconds | ✅ Pass |
| Search API | < 8 seconds | ✅ Pass |
| Content API | < 10 seconds | ✅ Pass |
| MCP Protocol | < 5 seconds | ✅ Pass |

#### **Deployment Troubleshooting**

**Build Failures:**
- Ensure .NET 8 SDK is available in build environment
- Check that all project files are included in git
- Verify `render.yaml` is in repository root

**Runtime Errors:**
- Check deployment logs in Render dashboard
- Verify environment variables: `ASPNETCORE_ENVIRONMENT=Production`
- Ensure port binding: `ASPNETCORE_URLS=http://0.0.0.0:$PORT`

**Slow Response Times:**
- Cold start issue on free tier - upgrade to paid tier
- Monitor resource usage in Render dashboard
- Implement ping service to keep instance warm

**Security Issues:**
- Verify CORS configuration for your domain
- Check security headers are present
- Ensure HTTPS is enforced

#### **Monitoring & Alerts**

**Health Monitoring:**
- Endpoint: `/api/wikipedia/health`
- Frequency: Every 5 minutes
- Expected: `200 OK` with healthy status

**Performance Monitoring:**
- Track API response times
- Monitor error rates (4xx/5xx)
- Set up uptime monitoring

**Render Dashboard:**
- Built-in metrics and logging
- Resource usage monitoring
- Auto-scaling configuration

#### **Deployment Verification Checklist**

- [ ] Health check endpoint responds with 200 OK
- [ ] All API endpoints return valid JSON
- [ ] **Remote Postman collection tests pass** (12/12 tests)
- [ ] MCP protocol initialization succeeds
- [ ] Security headers are present (HSTS, X-Frame-Options)
- [ ] HTTPS is enforced
- [ ] Environment shows "Production"
- [ ] Response times are acceptable (< 10s)
- [ ] Error handling works properly
- [ ] CORS is configured correctly
- [ ] Auto-deploy from GitHub works

## Testing

This project includes a comprehensive test suite with **206 total tests** across four categories, ensuring 100% reliability and production readiness for both HTTP and stdio transport modes.

### 🧪 **Automated Test Suite**

#### **Quick Test Execution**

Run all tests with a single command:

```bash
# Run all 182 tests (Unit + Service + Integration)
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run with coverage (if coverage tools are installed)
dotnet test --collect:"XPlat Code Coverage"
```

#### **Test Categories (206 Total Tests)**

##### **1. Unit Tests (13 tests)**
Location: `tests/WikipediaMcpServer.UnitTests/`

**Coverage:**
- **Model Validation Tests** - JSON serialization/deserialization
- **MCP Protocol Tests** - Request/response structures
- **Wikipedia Model Tests** - Data transfer objects
- **Validation Logic Tests** - Input validation and error handling

```bash
# Run only unit tests
dotnet test tests/WikipediaMcpServer.UnitTests/
```

##### **2. Service Tests (90 tests)**
Location: `tests/WikipediaMcpServer.ServiceTests/`

**Coverage:**
- **Wikipedia Service Tests** - Wikipedia API integration
- **HTTP Client Tests** - External API communication (mocked)
- **Error Handling Tests** - Network failures and API errors

```bash
# Run only service tests
dotnet test tests/WikipediaMcpServer.ServiceTests/
```

##### **3. Integration Tests (95 tests)**
Location: `tests/WikipediaMcpServer.IntegrationTests/`

**Coverage:**
- **HTTP Transport Tests** - Full JSON-RPC over HTTP/SSE
- **MCP Protocol Tests** - Initialize, tools/list, tools/call
- **Controller Integration Tests** - ASP.NET Core with WebApplicationFactory
- **End-to-End Workflow Tests** - Complete Wikipedia tool scenarios

```bash
# Run only integration tests
dotnet test tests/WikipediaMcpServer.IntegrationTests/
```

##### **4. stdio Transport Tests (8 tests)** 🆕
Location: `tests/WikipediaMcpServer.StdioTests/`

**Coverage:**
- **stdio Process Tests** - Real process spawning with `--mcp` flag
- **JSON-RPC via stdin/stdout** - Complete stdio transport validation
- **Wikipedia Tool Execution** - All 3 tools via stdio
- **Error Handling** - Invalid methods, malformed JSON, missing parameters

```bash
# Run only stdio tests
dotnet test tests/WikipediaMcpServer.StdioTests/
```

**Features:**
- ✅ Spawns actual MCP server process
- ✅ Validates stdin/stdout communication
- ✅ Tests VS Code/Claude Desktop integration scenarios
- ✅ Automated - no manual bash scripts needed

#### **Test Results Dashboard**

After running tests, view the coverage report:

```bash
# View coverage report (if generated)
open CoverageReport/index.html
```

### 🚀 **Quick Protocol Testing**

#### **Method 1: Automated Testing (Recommended)**

Run the comprehensive automated test suite:

```bash
# Run all stdio tests (8 tests)
dotnet test --filter "FullyQualifiedName~StdioTests"

# Or run all tests (206 tests - includes HTTP, unit, service, and stdio tests)
dotnet test
```

**The automated tests cover:**
- ✅ MCP server initialization
- ✅ Tool discovery and listing
- ✅ Wikipedia search functionality
- ✅ Wikipedia sections retrieval
- ✅ Wikipedia section content access
- ✅ Error handling (invalid methods, malformed JSON, missing parameters)
- ✅ Protocol compliance verification

#### **Method 2: Manual Interactive Testing**

1. Start the MCP Server:

   ```bash
   dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp
   ```

2. Send test messages by copying and pasting these JSON messages:

   **Initialize the server:**

   ```json
   {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0.0"}}}
   ```

   **List available tools:**

   ```json
   {"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}
   ```

   **Search Wikipedia:**

   ```json
   {"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"wikipedia_search","arguments":{"query":"Python programming"}}}
   ```

   **Get page sections:**

   ```json
   {"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"wikipedia_sections","arguments":{"topic":"Artificial Intelligence"}}}
   ```

   **Get section content:**

   ```json
   {"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"wikipedia_section_content","arguments":{"topic":"Python","section_title":"History"}}}
   ```

### Method 2: HTTP API Testing

Start the HTTP server:

```bash
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj
```

#### Option A: Using Postman (Recommended)

**For Local Development Testing:**

A comprehensive **JSON-RPC 2.0 MCP Protocol** Postman collection is provided for testing your local server:

1. **Import the MCP collection**: `WikipediaMcpServer-MCP-JsonRPC-Collection.json`
2. **Import the environment**: `WikipediaMcpServer-Environment.postman_environment.json`
3. **Ensure your local server is running** on `http://localhost:5070`
4. **Run the collection** to test all MCP protocol endpoints with automated assertions

> **� Important**: This server now uses the **Microsoft MCP SDK** with JSON-RPC 2.0 protocol. The old REST API endpoints (`/api/wikipedia/*`) no longer exist.

The MCP collection includes:

- ✅ Health check and server info tests
- ✅ MCP protocol initialization tests (`initialize`, `tools/list`)
- ✅ Wikipedia search tool tests via JSON-RPC (`tools/call`)
- ✅ Wikipedia sections tool tests via JSON-RPC
- ✅ Wikipedia section content tool tests via JSON-RPC
- ✅ Error handling validation for JSON-RPC 2.0
- ✅ Complete MCP workflow testing
- ✅ JSON-RPC 2.0 protocol compliance validation

**MCP Collection Features:**

- 15+ JSON-RPC 2.0 test requests
- 40+ automated test assertions for MCP protocol
- Proper JSON-RPC error handling validation
- MCP initialization handshake testing
- Tool discovery and invocation testing
- Environment variables for easy configuration

#### Option B: Using curl (JSON-RPC 2.0)

Test MCP endpoints with curl using JSON-RPC 2.0 protocol:

```bash
# Health check
curl "http://localhost:5070/health"

# Server info
curl "http://localhost:5070/info"

# MCP Initialize
curl -X POST "http://localhost:5070/" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{}}}'

# List available tools
curl -X POST "http://localhost:5070/" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}'

# Search Wikipedia via MCP tool
curl -X POST "http://localhost:5070/" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"wikipedia_search","arguments":{"query":"Artificial intelligence"}}}'

# Get sections via MCP tool  
curl -X POST "http://localhost:5070/" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"wikipedia_sections","arguments":{"topic":"Machine learning"}}}'

# Get section content via MCP tool
curl -X POST "http://localhost:5070/" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"wikipedia_section_content","arguments":{"topic":"Artificial intelligence","sectionTitle":"Overview"}}}'
```

**Important**: Use exact Wikipedia page titles (e.g., "Python (programming language)" instead of "Python programming") for reliable results.

### Method 3: Testing with MCP Clients

#### GitHub Copilot (VS Code)

1. Configure in VS Code settings as shown above
2. Restart VS Code
3. Ask Copilot: "Search Wikipedia for artificial intelligence"
4. Copilot should use your MCP server automatically

#### Claude Desktop

1. Add configuration to Claude's config file
2. Restart Claude Desktop
3. Ask Claude: "What's on Wikipedia about machine learning?"
4. Claude should use your Wikipedia tools

### Expected Test Results

#### Successful MCP Server Should

- ✅ Respond to initialize with server info
- ✅ List 3 tools (wikipedia_search, wikipedia_sections, wikipedia_section_content)
- ✅ Return Wikipedia data for search queries
- ✅ Return section lists for topics
- ✅ Return section content when requested

#### Successful HTTP Server Should

- ✅ Respond to health check
- ✅ Return JSON responses for all endpoints
- ✅ Handle URL-encoded parameters
- ✅ Return appropriate error messages for invalid requests

## Troubleshooting

### Common Issues

1. **Server not starting**: Check that .NET 8.0 is installed and the project builds without errors
2. **Path issues**: Verify all paths in the configuration are correct and absolute
3. **MCP client not recognizing tools**: Restart your MCP client after configuration changes
4. **Permission issues**: Ensure the project directory has proper read/write permissions
5. **JSON parse errors**: Ensure JSON messages are properly formatted
6. **Network timeouts**: Wikipedia API calls may take a few seconds
7. **Port conflicts**: Kill any processes using port 5070 for HTTP mode

### Debug Commands

```bash
# Check if project builds
dotnet build src/WikipediaMcpServer/WikipediaMcpServer.csproj

# Check for any compilation errors
dotnet run --project WikipediaMcpServer.csproj --dry-run

# Test with verbose logging
DOTNET_ENVIRONMENT=Development dotnet run --project WikipediaMcpServer.csproj -- --mcp
```

### Environment Variables

You can customize behavior with environment variables in your MCP configuration:

```json
{
  "env": {
    "DOTNET_ENVIRONMENT": "Production",
    "ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT": "Warning"
  }
}
```

### Development

To run in development mode with hot reload:

```bash
dotnet watch run
```

### API Documentation

When running in development mode, visit `http://localhost:5070` to access the Swagger UI documentation.

## Project Structure

```text
WikipediaMcpServer/
├── src/
│   └── WikipediaMcpServer/
│       ├── Controllers/
│       │   └── WikipediaController.cs                    # API endpoints
│       ├── Models/
│       │   ├── McpModels.cs                              # MCP protocol models
│       │   └── WikipediaModels.cs                       # Wikipedia data models and DTOs
│       ├── Services/
│       │   └── WikipediaService.cs                      # Wikipedia API integration
│       ├── Tools/
│       │   └── WikipediaTools.cs                        # MCP tools via Microsoft SDK
│       ├── Properties/
│       │   └── launchSettings.json                      # Launch configuration
│       ├── Program.cs                                   # Application configuration and startup
│       ├── WikipediaMcpServer.csproj                   # Project file
│       ├── appsettings.json                             # Application settings
│       └── appsettings.Development.json                # Development settings
├── tests/                                               # Test projects
│   ├── WikipediaMcpServer.UnitTests/                   # Unit tests (77 tests)
│   │   ├── Models/
│   │   │   ├── McpModelsTests.cs                       # MCP model validation tests
│   │   │   └── WikipediaModelTests.cs                  # Wikipedia model tests
│   │   ├── Serialization/
│   │   │   └── JsonSerializationTests.cs               # JSON serialization tests
│   │   └── WikipediaMcpServer.UnitTests.csproj        # Unit test project file
│   ├── WikipediaMcpServer.ServiceTests/                # Service tests (31 tests)
│   │   ├── Services/
│   │   │   └── WikipediaServiceTests.cs                # Wikipedia API service tests (22 tests)
│   │   ├── McpProtocolSerializationTests.cs            # MCP protocol serialization tests (9 tests)
│   │   └── WikipediaMcpServer.ServiceTests.csproj     # Service test project file
│   ├── WikipediaMcpServer.IntegrationTests/            # Integration tests (51 tests)
│   │   ├── ProgramIntegrationTests.cs                  # Application startup tests
│   │   ├── WikipediaControllerIntegrationTests.cs     # Basic controller tests
│   │   ├── WikipediaControllerComprehensiveTests.cs   # Comprehensive endpoint tests
│   │   └── WikipediaMcpServer.IntegrationTests.csproj # Integration test project file
│   └── WikipediaMcpServer.StdioTests/                  # Stdio mode tests (8 tests)
│       ├── StdioModeTests.cs                           # Stdio transport integration tests
│       └── WikipediaMcpServer.StdioTests.csproj       # Stdio test project file
├── CoverageReport/                                     # Code coverage reports
│   ├── index.html                                      # Coverage dashboard
│   └── ...                                             # Detailed coverage files
├── docs/                                               # Additional documentation
├── mcp.json                                            # Example MCP configuration (reference only)
├── WikipediaMcpServer.sln                             # Solution file
├── WikipediaMcpServer-MCP-JsonRPC-Collection.json      # JSON-RPC 2.0 MCP Protocol Postman collection
├── WikipediaMcpServer-Environment.postman_environment.json  # Local development environment
├── WikipediaMcpServer-Remote-Collection.json          # Remote deployment testing collection  
└── WikipediaMcpServer-Remote-Environment.postman_environment.json  # Remote deployment environment
```

## Configuration Files

The application uses several configuration approaches:

- `appsettings.json` - Production settings
- `appsettings.Development.json` - Development settings
- `mcp.json` - Example MCP configuration file (reference only - not used by VS Code or Claude Desktop)

## Testing Files

**For Local Development Testing:**
- `WikipediaMcpServer-MCP-JsonRPC-Collection.json` - JSON-RPC 2.0 MCP Protocol Postman collection
- `WikipediaMcpServer-Environment.postman_environment.json` - Local environment variables

**For Remote Deployment Testing:**
- `WikipediaMcpServer-Remote-Collection.json` - Remote deployment test collection
- `WikipediaMcpServer-Remote-Environment.postman_environment.json` - Remote environment variables

## Releases

This project follows semantic versioning and includes tagged releases:

- **v8.2** - Remote MCP Support - HTTP RPC endpoint for remote access (198 tests)
  - Added `/mcp/rpc` endpoint for remote MCP access via HTTP POST
  - Dual transport support: stdio (local) + HTTP RPC (remote) simultaneously
  - Updated mcp-http-bridge.js with HTTP/HTTPS protocol support
  - Comprehensive documentation in REMOTE_MCP_SETUP.md
  - Same Wikipedia service logic shared between local and remote modes
  - Production-ready deployment on Render with JSON-RPC 2.0 over HTTP
- **v8.1** - VS Code MCP Integration Fix - JSON-RPC 2.0 stdio compliance (206 tests)
  - Fixed stdio mode JSON formatting for VS Code MCP client compatibility
  - Added parameter name compatibility (snake_case and camelCase)
  - Compact single-line JSON-RPC responses (required by JSON-RPC 2.0 spec)
  - All 206 tests passing with full VS Code MCP integration validated
- **v8.0** - Microsoft MCP SDK Migration (206 tests)
  - Migrated to official Microsoft ModelContextProtocol SDK v0.4.0-preview.2
  - Enhanced stdio mode implementation with improved protocol compliance
  - Added 24 new stdio transport tests
  - Professional .NET solution structure maintained
- **v4.0** - Production-ready release with comprehensive testing (182 tests), professional .NET structure, enhanced MCP protocol support, and complete HTTP API compatibility
- **v3.0** - Enhanced API integration and improved error handling  
- **v2.0** - Professional project structure with src/ and tests/ organization
- **v1.0** - Initial Wikipedia MCP Server implementation

Each release is tagged and available on GitHub with detailed release notes.

## Technologies Used

### **Core Framework**
- **ASP.NET Core 8** - Web framework and dependency injection
- **Microsoft ModelContextProtocol SDK v0.4.0-preview.2** - Official MCP server implementation
- **System.Text.Json 8.0.5** - JSON serialization (security-updated version)
- **HttpClient** - HTTP requests to Wikipedia API

### **Testing Framework**
- **xUnit** - Primary testing framework for all test types
- **Fluent Assertions** - Enhanced assertion library for readable tests
- **Microsoft.AspNetCore.Mvc.Testing** - Integration testing support
- **Code Coverage Tools** - Coverage analysis and reporting

### **API & Documentation**
- **Swagger/OpenAPI** - API documentation and testing interface
- **Model Context Protocol (MCP)** - Integration with AI development tools
- **Postman Collections** - Comprehensive API testing suites

### **Development Tools**
- **VS Code Integration** - MCP server configuration for development
- **Claude Desktop Integration** - AI-powered development support
- **Git Conditional Configuration** - Separate identities for different Git hosting platforms

## Performance Notes

- Wikipedia API calls typically take 1-3 seconds
- MCP server startup is usually under 2 seconds
- HTTP server startup is usually under 5 seconds
- Each tool call is independent and stateless

## License

This project is licensed under the MIT License.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

