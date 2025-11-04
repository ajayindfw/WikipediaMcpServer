# Wikipedia MCP Server - Troubleshooting Guide

This comprehensive troubleshooting guide addresses common issues you may encounter when setting up and using the Wikipedia MCP Server.

## 🚨 Common Issues

### "Failed to Parse Message" Errors

This is the most common issue when integrating with VS Code MCP extension.

#### Complete Reset Procedure

**Step 1: Kill All Existing Processes**
```bash
# Kill any existing Wikipedia MCP processes
pkill -f "dotnet.*WikipediaMcp"
pkill -f "WikipediaMcpServer"

# Verify no processes are running
ps aux | grep -i "dotnet.*WikipediaMcp" | grep -v grep
```

**Step 2: Force VS Code to Reset MCP Connections**
1. **Open Command Palette**: `Cmd+Shift+P` (macOS) or `Ctrl+Shift+P` (Windows/Linux)
2. **Run**: `Developer: Reload Window` (this forces VS Code to restart)
3. **Wait** for VS Code to fully reload

**Step 3: Restart MCP Server Fresh**
1. **Open Command Palette**: `Cmd+Shift+P`
2. **Type**: `MCP: Restart Server`
3. **Select**: `wikipedia-local`
4. **Wait** for connection indicator in status bar

**Step 4: Verify Connection Status**
Look at the **bottom status bar** in VS Code:
- ✅ **Green indicator**: MCP server connected successfully
- ❌ **Red indicator**: Connection failed
- 🟡 **Yellow indicator**: Connecting...

**Step 5: Check MCP Logs**
1. **Open Command Palette**: `Cmd+Shift+P`
2. **Type**: `MCP: Show Server Logs`
3. **Select**: `wikipedia-local`
4. **Look for**: Any error messages or "failed to parse" entries

#### Test the Connection

Try this simple test prompt in VS Code chat:
```
Search Wikipedia for "test" and tell me what you find.
```

#### Advanced Debugging

**VS Code Developer Console:**
1. **Open**: `Help` → `Toggle Developer Tools` → `Console` tab
2. **Look for**: Any MCP-related error messages
3. **Check**: Console errors related to JSON parsing

**Manual stdio Test:**
```bash
cd /path/to/WikipediaMcpServer
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{}}}' | dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp
```

Should return clean JSON response without errors.

**Alternative VS Code Settings:**
Add this to your VS Code settings.json temporarily:
```json
{
  "mcp.logging.level": "debug",
  "mcp.timeout": 30000
}
```

#### Expected Behavior After Fix

You should see in the MCP logs:
- **🔵 Blue**: Request logging (when VS Code sends requests)
- **🟢 Green**: Response logging (server responses)
- **🟣 Magenta**: Method names being called
- **No parse errors**: Clean JSON-RPC communication

---

## 🔧 Build and Compilation Issues

### .NET SDK Not Found

**Error**: `SDK 'Microsoft.NET.Sdk.Web' not found`

**Solution**:
```bash
# Check .NET version
dotnet --version

# Install .NET 8.0 SDK if not present
# macOS (Homebrew):
brew install dotnet@8

# Windows: Download from https://dotnet.microsoft.com/download/dotnet/8.0
# Linux (Ubuntu): 
sudo apt-get install -y dotnet-sdk-8.0
```

### Package Restore Failures

**Error**: Package restore fails or dependency conflicts

**Solution**:
```bash
# Clear all NuGet caches
dotnet nuget locals all --clear

# Restore packages
dotnet restore

# Clean and rebuild
dotnet clean
dotnet build
```

### Multiple .NET Versions Conflict

**Issue**: Project builds with wrong .NET version

**Solution**:
Check `global.json` exists and specifies .NET 8:
```json
{
  "sdk": {
    "version": "8.0.0",
    "rollForward": "latestMajor"
  }
}
```

---

## 🌐 Network and Connection Issues

### Port 5070 Already in Use

**Error**: Address already in use: localhost:5070

**Solution**:
```bash
# Find process using port 5070
lsof -ti:5070

# Kill the process
lsof -ti:5070 | xargs kill -9

# Or use a different port
export ASPNETCORE_URLS=http://localhost:5071
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj
```

### Wikipedia API Timeouts

**Error**: HTTP timeouts when calling Wikipedia API

**Solutions**:
1. **Check internet connection**
2. **Verify Wikipedia API status**: https://en.wikipedia.org/api/rest_v1/
3. **Increase timeout in appsettings.json**:
```json
{
  "Wikipedia": {
    "Timeout": "00:01:00"
  }
}
```

### CORS Errors in Browser

**Error**: CORS policy errors when testing with web clients

**Solution**: 
The server already includes CORS configuration. If still getting errors:
```bash
# Check if running in development mode
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj
```

---

## 📟 stdio Mode Issues

### Server Starts in HTTP Mode Instead of stdio

**Problem**: Server shows web server startup messages instead of stdio mode

**Solution**: Ensure you're passing the `--mcp` flag correctly:
```bash
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp
#                                                                      ^^^^ Note the -- separator
```

### No Response from stdio Commands

**Problem**: Commands sent to stdin don't produce responses

**Debug checklist**:
1. ✅ Verify JSON-RPC format is correct
2. ✅ Check that `id` field is present in request
3. ✅ Ensure no extra whitespace or newlines
4. ✅ Verify method names are exact: `initialize`, `tools/list`, `tools/call`

**Test with simple initialization**:
```bash
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}' | dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp
```

### Logging Interference

**Problem**: ASP.NET Core logs interfering with stdio output

**Root Cause**: ASP.NET Core logging was being sent to stderr, which VS Code MCP tried to parse as JSON-RPC.

**Solution Implemented**: Complete logging suppression in stdio mode:
```csharp
// In stdio mode, suppress all ASP.NET Core logging
LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.None))
```

---

## 🧪 Testing Issues

### Tests Failing

**Issue**: Unit, integration, or stdio tests failing

**Debugging steps**:
```bash
# Run tests with verbose output
dotnet test --verbosity normal

# Run specific test category
dotnet test --filter "FullyQualifiedName~StdioTests"
dotnet test --filter "FullyQualifiedName~IntegrationTests"

# Check for compilation errors
dotnet build tests/WikipediaMcpServer.StdioTests/
```

### Postman Collection Issues

**Issue**: Postman requests failing or returning errors

**Solutions**:
1. **Check server is running**: `curl http://localhost:5070/health`
2. **Verify endpoint URL**: Use `/mcp/rpc` for JSON-RPC
3. **Check request format**: Must be valid JSON-RPC 2.0
4. **Import latest collections**: Use updated collection files

**Example working request**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/list",
  "params": {}
}
```

---

## 🎯 VS Code Integration Issues

### MCP Extension Not Finding Server

**Problem**: VS Code MCP extension doesn't show Wikipedia server

**Checklist**:
1. ✅ Is `mcp.json` in the correct location?
   - **macOS**: `~/Library/Application Support/Code/User/mcp.json`
   - **Windows**: `%APPDATA%\Code\User\mcp.json`
   - **Linux**: `~/.config/Code/User/mcp.json`

2. ✅ Is the server path absolute or using `${workspaceFolder}`?
3. ✅ Is `--mcp` flag in the args array?
4. ✅ Are there any syntax errors in the JSON?

**Test configuration**:
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

### Tools Not Appearing

**Problem**: MCP server connects but tools don't appear

**Solutions**:
1. **Check server logs** for tool registration errors
2. **Verify tools/list response**:
```bash
echo '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' | dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp
```
3. **Expected response**: Should list 3 Wikipedia tools

### Connection Timeouts

**Problem**: VS Code MCP connection times out

**Solutions**:
1. **Increase timeout** in VS Code settings:
```json
{
  "mcp.timeout": 30000
}
```

2. **Check server startup time**:
```bash
time dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp
```

---

## 🌐 Remote Deployment Issues

### Render Deployment Fails

**Problem**: Deployment to Render fails during build

**Common solutions**:
1. **Check `render.yaml`** configuration
2. **Verify build command**: `dotnet publish src/WikipediaMcpServer/WikipediaMcpServer.csproj -c Release -o ./publish`
3. **Check start command**: `dotnet ./publish/WikipediaMcpServer.dll`
4. **Environment variables**: Ensure `ASPNETCORE_URLS=http://0.0.0.0:$PORT`

### Remote Server Not Responding

**Problem**: Deployed server not accessible

**Debug steps**:
```bash
# Test health endpoint
curl https://your-app.onrender.com/health

# Test info endpoint
curl https://your-app.onrender.com/info

# Check Render logs in dashboard
```

### Bridge Script Issues

**Problem**: `mcp-http-bridge.js` not working

**Solutions**:
1. **Verify Node.js**: `node --version`
2. **Check script permissions**: `chmod +x mcp-http-bridge.js`
3. **Test bridge manually**:
```bash
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' | node mcp-http-bridge.js
```

---

## 🔍 Logging and Debugging

### Enable Debug Logging

**For development**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**For stdio mode debugging**:
```bash
export MCP_DEBUG=true
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp
```

### Response Logging Enhancement

The server now logs full response content for debugging:

**Enhanced Terminal Output**:
```
📥 MCP HTTP Request: {"jsonrpc":"2.0","id":1,"method":"initialize"...
🎯 MCP Method: initialize
📤 MCP HTTP Response: {"jsonrpc":"2.0","id":1,"result":{"protocolVersion":"2024-11-05"...}}
📤 Response sent (Protocol: 2024-11-05, Length: 145 chars)
```

**Features**:
- **Full Response Content**: Shows actual JSON being returned
- **Smart Truncation**: Responses >500 chars show with "..."
- **Length Information**: Character count of full response
- **Protocol Awareness**: Shows negotiated MCP protocol version
- **Error Logging**: Error responses with actual content

---

## 📋 Diagnostic Commands

### System Check

```bash
# Check .NET version
dotnet --version

# Check project builds
dotnet build src/WikipediaMcpServer/WikipediaMcpServer.csproj

# Check tests pass
dotnet test --verbosity quiet

# Check server starts (HTTP mode)
timeout 10s dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj &
curl http://localhost:5070/health
pkill -f WikipediaMcpServer
```

### Network Check

```bash
# Check if port is available
lsof -i :5070

# Test local connectivity
curl -I http://localhost:5070/health

# Test remote connectivity (if deployed)
curl -I https://your-deployment.onrender.com/health
```

### JSON-RPC Validation

```bash
# Test initialize
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}' | dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp

# Test tools list
echo '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' | dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp
```

---

## 🆘 Still Having Issues?

### Collect Debug Information

If you're still experiencing issues, collect this information:

1. **Environment**:
   - OS version
   - .NET version: `dotnet --version`
   - VS Code version
   - MCP extension version

2. **Configuration**:
   - Your `mcp.json` configuration
   - Project file location
   - Environment variables

3. **Logs**:
   - VS Code MCP extension logs
   - Server console output
   - Any error messages

4. **Test Results**:
   - Result of manual stdio test
   - Health check response
   - Build/test output

### Common Resolution Patterns

Most issues fall into these categories:

1. **Path Issues** (40%): Incorrect absolute paths in `mcp.json`
2. **Process Issues** (25%): Multiple servers running or zombie processes
3. **Configuration Issues** (20%): Missing `--mcp` flag or JSON syntax errors
4. **Environment Issues** (10%): Wrong .NET version or missing dependencies
5. **Network Issues** (5%): Port conflicts or firewall issues

### Reset Everything

If all else fails, complete reset:

```bash
# 1. Kill all processes
pkill -f dotnet
pkill -f WikipediaMcp

# 2. Clean project
cd /path/to/WikipediaMcpServer
dotnet clean
rm -rf bin obj

# 3. Fresh build
dotnet restore
dotnet build

# 4. Test stdio mode
dotnet test --filter "FullyQualifiedName~StdioTests"

# 5. Restart VS Code completely
```

Following this troubleshooting guide should resolve most issues you encounter with the Wikipedia MCP Server.