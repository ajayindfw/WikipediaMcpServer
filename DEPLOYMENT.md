# Deployment Guide for Wikipedia MCP Server

This guide covers deploying the Wikipedia MCP Server to various cloud platforms.

## 🚀 **Quick Deployment Options**

### **Option 1: Render (Recommended for beginners)**

1. **Fork or Clone** this repository to your GitHub account
2. **Create a Render account** at [render.com](https://render.com)
3. **Connect your GitHub** repository to Render
4. **Create a new Web Service** from your repository
5. Render will **automatically detect** the `render.yaml` configuration
6. **Deploy** with one click!

**Configuration:**
- Build Command: `dotnet restore src/WikipediaMcpServer/WikipediaMcpServer.csproj && dotnet publish src/WikipediaMcpServer/WikipediaMcpServer.csproj -c Release -o ./publish`
- Start Command: `dotnet ./publish/WikipediaMcpServer.dll`
- Environment: Set `ASPNETCORE_URLS=http://0.0.0.0:$PORT`

### **Option 2: Railway**

1. **Install Railway CLI** or use the web interface
2. **Login** to Railway: `railway login`
3. **Initialize** project: `railway init`
4. **Deploy**: `railway up`
5. Railway will use the `railway.toml` configuration automatically

**Quick Commands:**
```bash
npm install -g @railway/cli
railway login
railway init
railway up
```

### **Option 3: Docker (Any Platform)**

Build and run locally or deploy to any Docker-compatible platform:

```bash
# Build the Docker image
docker build -t wikipedia-mcp-server .

# Run locally
docker run -p 5070:8080 wikipedia-mcp-server

# Or use Docker Compose
docker-compose up -d
```

### **Option 4: Azure Container Instances**

```bash
# Build and push to Azure Container Registry
az acr build --registry myregistry --image wikipedia-mcp-server .

# Deploy to Container Instances
az container create \
  --resource-group myResourceGroup \
  --name wikipedia-mcp-server \
  --image myregistry.azurecr.io/wikipedia-mcp-server:latest \
  --ports 8080 \
  --environment-variables ASPNETCORE_ENVIRONMENT=Production
```

### **Option 5: Google Cloud Run**

```bash
# Build and deploy in one command
gcloud run deploy wikipedia-mcp-server \
  --source . \
  --platform managed \
  --region us-central1 \
  --allow-unauthenticated
```

## 🔧 **Configuration Options**

### **Environment Variables**

Set these environment variables in your deployment platform:

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | ASP.NET Core environment |
| `ASPNETCORE_URLS` | `http://+:8080` | Binding URLs |
| `DOTNET_ENVIRONMENT` | `Production` | .NET environment |
| `MCP_MODE` | `false` | Set to `true` for MCP-only mode |

### **Health Check Endpoint**

All platforms can use this health check endpoint:
- **Path**: `/api/wikipedia/health`
- **Method**: `GET`
- **Expected Response**: `200 OK` with JSON status

### **Port Configuration**

- **Default Port**: `8080` (configurable via `ASPNETCORE_URLS`)
- **Health Check**: Same port as application
- **Protocol**: HTTP (HTTPS termination handled by platform)

## 🌐 **Platform-Specific Instructions**

### **Render Deployment**

1. **GitHub Integration**:
   - Connect your GitHub repository
   - Render auto-detects the `render.yaml` file
   - Automatic deployments on git push

2. **Manual Configuration** (if not using render.yaml):
   - Build Command: `dotnet publish src/WikipediaMcpServer/WikipediaMcpServer.csproj -c Release -o ./publish`
   - Start Command: `dotnet ./publish/WikipediaMcpServer.dll`
   - Add environment variables in Render dashboard

3. **Custom Domain**:
   - Add your domain in Render dashboard
   - Update DNS to point to Render

### **Railway Deployment**

1. **CLI Deployment**:
   ```bash
   railway login
   railway init
   railway up
   ```

2. **GitHub Integration**:
   - Connect repository in Railway dashboard
   - Auto-deploy on push

3. **Environment Variables**:
   - Set in Railway dashboard or via CLI:
   ```bash
   railway variables set ASPNETCORE_ENVIRONMENT=Production
   ```

### **Docker Deployment**

1. **Build Options**:
   ```bash
   # Standard build
   docker build -t wikipedia-mcp-server .
   
   # Multi-platform build
   docker buildx build --platform linux/amd64,linux/arm64 -t wikipedia-mcp-server .
   ```

2. **Registry Push**:
   ```bash
   # Tag for registry
   docker tag wikipedia-mcp-server your-registry/wikipedia-mcp-server:latest
   
   # Push to registry
   docker push your-registry/wikipedia-mcp-server:latest
   ```

## 🔍 **Testing Your Deployment**

### **Health Check**
```bash
curl https://your-deployment-url.com/api/wikipedia/health
```

### **API Testing**
```bash
# Test Wikipedia search
curl "https://your-deployment-url.com/api/wikipedia/search?query=Python"

# Test sections endpoint
curl "https://your-deployment-url.com/api/wikipedia/sections?topic=Artificial%20intelligence"
```

### **MCP Integration**

Update your MCP client configuration to use the deployed URL:

```json
{
  "mcpServers": {
    "wikipedia-remote": {
      "command": "curl",
      "args": [
        "-X", "POST",
        "https://your-deployment-url.com/mcp",
        "-H", "Content-Type: application/json",
        "-d", "@-"
      ]
    }
  }
}
```

## 🛠 **Troubleshooting**

### **Common Issues**

1. **Build Failures**:
   - Ensure .NET 8 SDK is available in build environment
   - Check that all project files are included in git

2. **Runtime Errors**:
   - Verify environment variables are set correctly
   - Check logs for detailed error messages

3. **Port Binding Issues**:
   - Ensure `ASPNETCORE_URLS` uses `0.0.0.0` not `localhost`
   - Platform port must match `$PORT` environment variable

### **Platform-Specific Troubleshooting**

**Render**:
- Check build logs in Render dashboard
- Verify `render.yaml` is in repository root

**Railway**:
- Use `railway logs` to view application logs
- Check service status with `railway status`

**Docker**:
- Test locally first: `docker run -p 5070:8080 wikipedia-mcp-server`
- Check container logs: `docker logs container-name`

## 🔒 **Security Considerations**

1. **HTTPS**: All platforms provide automatic HTTPS termination
2. **Environment Variables**: Never commit secrets to git
3. **Health Checks**: Use provided health check endpoint
4. **Rate Limiting**: Consider implementing rate limiting for production use

## 💰 **Cost Considerations**

| Platform | Free Tier | Paid Plans Start |
|----------|-----------|------------------|
| Render | ✅ 750 hours/month | $7/month |
| Railway | ✅ $5 credit/month | Pay-as-you-go |
| Google Cloud Run | ✅ 2 million requests/month | Pay-as-you-go |
| Azure Container Instances | ❌ | ~$13/month |

## 📈 **Scaling**

All platforms support automatic scaling based on traffic:
- **Render**: Horizontal scaling on paid plans
- **Railway**: Automatic scaling with usage-based pricing
- **Cloud Run**: Automatic scaling from 0 to 1000+ instances
- **Container Instances**: Manual scaling, supports scale sets

Your Wikipedia MCP Server is now ready for deployment! Choose the platform that best fits your needs and budget. 🚀