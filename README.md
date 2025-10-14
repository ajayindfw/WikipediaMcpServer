# Wikipedia MCP Server (ASP.NET Core 8)

A Model Context Protocol (MCP) server implementation for Wikipedia search and content retrieval, built with ASP.NET Core 8 and C#.

## Features

This server provides three main Wikipedia-related tools:

1. **Wikipedia Search** - Search for Wikipedia articles and get summaries
2. **Wikipedia Sections** - Get the section outline of a Wikipedia page  
3. **Wikipedia Section Content** - Retrieve content from specific sections of Wikipedia articles

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
   dotnet build
   ```

## Usage Modes

This server can run in two modes:

1. **HTTP API Server** - Traditional REST API endpoints
2. **MCP Server** - Model Context Protocol server for AI integration

### Running as HTTP API Server

To run as a traditional HTTP API server:

```bash
dotnet run
```

The server will start on `http://localhost:5070` by default.

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

#### Option 1: Using mcp.json File

Create a `mcp.json` file in your project root:

```json
{
  "mcpServers": {
    "wikipedia-dotnet": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/full/path/to/your/WikipediaMcpServer.csproj",
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

**Important**: Update the path in the `args` array to match your actual project location.

#### Option 2: VS Code Settings Configuration

1. Open VS Code Settings (JSON): `Cmd/Ctrl + Shift + P` → "Preferences: Open User Settings (JSON)"

2. Add the MCP configuration:

   ```json
   {
     "github.copilot.chat.mcp.servers": {
       "wikipedia-dotnet": {
         "command": "dotnet",
         "args": [
           "run",
           "--project",
           "/full/path/to/your/WikipediaMcpServer.csproj",
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

#### Option 3: VS Code MCP Configuration File

1. Open VS Code's global MCP configuration file:
   - **macOS**: `~/Library/Application Support/Code/User/mcp.json`
   - **Windows**: `%APPDATA%/Code/User/mcp.json`
   - **Linux**: `~/.config/Code/User/mcp.json`

2. Add the Wikipedia MCP server to your configuration:

   ```json
   {
     "servers": {
       "wikipedia-dotnet": {
         "command": "dotnet",
         "type": "stdio",
         "args": [
           "run",
           "--project",
           "/full/path/to/your/WikipediaMcpServer.csproj",
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

#### Option 4: Claude Desktop Configuration

To use with Claude Desktop:

1. Open Claude Desktop's configuration file:
   - **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
   - **Windows**: `%APPDATA%/Claude/claude_desktop_config.json`

2. Add the server configuration:

   ```json
   {
     "mcpServers": {
       "wikipedia-dotnet": {
         "command": "dotnet",
         "args": [
           "run",
           "--project",
           "/full/path/to/your/WikipediaMcpServer.csproj",
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

3. Restart Claude Desktop to load the new configuration.

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

## Testing

### Method 1: Manual Interactive Testing (Recommended)

1. Start the MCP Server:

   ```bash
   dotnet run --project WikipediaMcpServer.csproj -- --mcp
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
dotnet run --project WikipediaMcpServer.csproj
```

#### Option A: Using Postman (Recommended)

A comprehensive Postman collection is provided for testing all endpoints:

1. **Import the Postman collection**: `WikipediaMcpServer-Postman-Collection.json`
2. **Import the environment**: `WikipediaMcpServer-Environment.postman_environment.json`
3. **Run the collection** to test all endpoints with automated assertions

The collection includes:

- ✅ Health check tests
- ✅ Search functionality tests (with exact Wikipedia page titles)
- ✅ Sections retrieval tests
- ✅ Section content tests
- ✅ Error handling validation
- ✅ Response time and format validation
- ✅ Complete workflow testing

**Collection Features:**

- 16 comprehensive test requests
- 50+ automated test assertions
- Proper error handling validation
- Performance testing
- Environment variables for easy configuration

#### Option B: Using curl

Test endpoints with curl:

```bash
# Health check
curl "http://localhost:5070/api/wikipedia/health"

# Search Wikipedia (use exact Wikipedia page titles)
curl "http://localhost:5070/api/wikipedia/search?query=Artificial%20intelligence"
curl "http://localhost:5070/api/wikipedia/search?query=Python%20%28programming%20language%29"

# Get sections
curl "http://localhost:5070/api/wikipedia/sections?topic=Python%20%28programming%20language%29"

# Get section content
curl "http://localhost:5070/api/wikipedia/section-content?topic=Python%20%28programming%20language%29&sectionTitle=History"
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
dotnet build WikipediaMcpServer.csproj

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
├── Controllers/
│   └── WikipediaController.cs                    # API endpoints
├── Models/
│   ├── McpModels.cs                              # MCP protocol models
│   └── WikipediaModels.cs                       # Wikipedia data models and DTOs
├── Services/
│   ├── McpServerService.cs                      # MCP protocol handling
│   └── WikipediaService.cs                      # Wikipedia API integration
├── Properties/
│   └── launchSettings.json                      # Launch configuration
├── Program.cs                                   # Application configuration and startup
├── WikipediaMcpServer.csproj                   # Project file
├── mcp.json                                     # MCP server configuration
├── appsettings.json                             # Application settings
├── WikipediaMcpServer-Postman-Collection.json  # Postman test collection
└── WikipediaMcpServer-Environment.postman_environment.json  # Postman environment
```

## Configuration Files

The application uses several configuration approaches:

- `appsettings.json` - Production settings
- `appsettings.Development.json` - Development settings
- `mcp.json` - MCP server configuration (can be used by various MCP clients)

## Testing Files

For HTTP API testing:

- `WikipediaMcpServer-Postman-Collection.json` - Comprehensive Postman test collection
- `WikipediaMcpServer-Environment.postman_environment.json` - Postman environment variables

## Technologies Used

- **ASP.NET Core 8** - Web framework
- **Swagger/OpenAPI** - API documentation
- **HttpClient** - HTTP requests to Wikipedia API
- **System.Text.Json 8.0.5** - JSON serialization (security-updated version)
- **Model Context Protocol (MCP)** - Integration with AI development tools

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

