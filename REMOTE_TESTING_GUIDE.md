# 🌐 Remote Deployment Testing Guide

This guide covers testing your Wikipedia MCP Server after deploying to remote platforms like Render, Railway, or other cloud services.

## 📋 **Quick Start: Testing Your Remote Deployment**

### **Step 1: Import Postman Collections**

1. **Import the Remote Testing Collection**:
   - File: `WikipediaMcpServer-Remote-Collection.json`
   - This collection is specifically designed for remote deployment testing

2. **Import the Remote Environment**:
   - File: `WikipediaMcpServer-Remote-Environment.json`
   - Contains environment variables for different deployment platforms

### **Step 2: Configure Your Deployment URL**

In Postman, update the environment variable:
- **Variable**: `base_url`
- **Value**: Your actual deployment URL (e.g., `https://your-service-name.onrender.com`)

### **Step 3: Run the Test Suite**

Execute the collection using Postman Runner:
1. Click **"Run Collection"**
2. Select **"Wikipedia MCP Server - Remote Environments"**
3. Run all tests to verify your deployment

## 🧪 **Test Categories**

### **🚀 Deployment Health Checks**
- **Health Check**: Verifies `/api/wikipedia/health` endpoint
- **Root Endpoint**: Tests API information and environment detection
- **Expected Response Time**: < 5 seconds

### **🔍 Wikipedia Search API**
- **Basic Search**: Tests search functionality with simple queries
- **Advanced Search**: Tests with limits and parameters
- **Expected Response Time**: < 10 seconds

### **📄 Wikipedia Content API**
- **Page Sections**: Tests section listing functionality
- **Section Content**: Tests content retrieval
- **Data Validation**: Ensures proper JSON structure

### **🧪 MCP Protocol Tests**
- **MCP Initialize**: Tests JSON-RPC initialization
- **MCP List Tools**: Verifies tool discovery
- **Protocol Compliance**: Ensures MCP 2024-11-05 compatibility

### **⚡ Performance & Load Tests**
- **Concurrent Requests**: Tests handling multiple simultaneous requests
- **Response Times**: Validates performance under load
- **Random Query Testing**: Uses dynamic test data

### **🔒 Security & Production Tests**
- **CORS Headers**: Verifies proper CORS configuration
- **Security Headers**: Checks HSTS, X-Frame-Options, etc.
- **HTTPS Enforcement**: Ensures secure connections

## 🎯 **Platform-Specific Testing**

### **Render Testing**
```bash
# Your Render URL format
https://wikipedia-mcp-server-[hash].onrender.com

# Expected features:
✅ Auto-deploy from GitHub
✅ Health check monitoring
✅ Production environment
✅ HTTPS enabled
```

### **Railway Testing**
```bash
# Your Railway URL format
https://your-app-name.railway.app

# Expected features:
✅ Custom domain support
✅ Environment variables
✅ Build logs access
✅ Auto-scaling
```

### **Docker Testing**
```bash
# Health check command
curl https://your-docker-host/api/wikipedia/health

# Expected Docker features:
✅ Non-root user execution
✅ Multi-stage build
✅ Security best practices
✅ Health check endpoint
```

## 📊 **Expected Test Results**

### **Successful Deployment Indicators**
- ✅ All health checks return `200 OK`
- ✅ API responses contain valid JSON
- ✅ Environment shows `"Production"`
- ✅ Security headers are present
- ✅ Response times < 10 seconds

### **Performance Benchmarks**
| Test Type | Expected Time | Status |
|-----------|---------------|---------|
| Health Check | < 2 seconds | ✅ Pass |
| Search API | < 8 seconds | ✅ Pass |
| Content API | < 10 seconds | ✅ Pass |
| MCP Protocol | < 5 seconds | ✅ Pass |

## 🐛 **Troubleshooting Remote Deployments**

### **Common Issues & Solutions**

#### **❌ Health Check Fails (404/500)**
```bash
# Check deployment logs
# Render: Dashboard > Service > Logs
# Railway: railway logs --follow

# Verify environment variables:
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:$PORT
```

#### **❌ Slow Response Times (> 15 seconds)**
```bash
# Check if cold start issue (free tier)
# Solution: Upgrade to paid tier or implement ping service

# Monitor resource usage in platform dashboard
```

#### **❌ CORS Errors**
```bash
# Verify CORS configuration in Program.cs
# Check if custom domains require additional CORS setup
```

#### **❌ Security Header Missing**
```bash
# Ensure production security middleware is active
# Check Program.cs for security headers configuration
```

## 🔄 **Continuous Testing**

### **Automated Testing Pipeline**
1. **Deploy**: Push to GitHub triggers auto-deploy
2. **Test**: Run Postman collection via Newman CLI
3. **Monitor**: Set up health check monitoring
4. **Alert**: Configure notifications for failures

### **Newman CLI Testing**
```bash
# Install Newman
npm install -g newman

# Run remote tests
newman run WikipediaMcpServer-Remote-Collection.json \
  -e WikipediaMcpServer-Remote-Environment.json \
  --reporters cli,json \
  --reporter-json-export results.json
```

## 📈 **Monitoring & Alerts**

### **Health Check Monitoring**
- **Endpoint**: `https://your-deployment.com/api/wikipedia/health`
- **Frequency**: Every 5 minutes
- **Expected**: `200 OK` with `{"status": "Healthy"}`

### **Performance Monitoring**
- **Response Time**: Monitor API response times
- **Error Rate**: Track 4xx/5xx responses
- **Uptime**: Measure service availability

### **Platform-Specific Monitoring**
- **Render**: Built-in metrics dashboard
- **Railway**: Resource usage monitoring
- **Docker**: Container health checks

## ✅ **Deployment Verification Checklist**

- [ ] Health check endpoint responds
- [ ] API search functionality works
- [ ] MCP protocol initialization succeeds
- [ ] Security headers are present
- [ ] HTTPS is enforced
- [ ] Environment shows "Production"
- [ ] Response times are acceptable
- [ ] Error handling works properly
- [ ] CORS is configured correctly
- [ ] Auto-deploy from GitHub works

## 🎉 **Success!**

Once all tests pass, your Wikipedia MCP Server is successfully deployed and ready for production use!

**Next Steps:**
1. Set up monitoring and alerts
2. Configure custom domains (if needed)
3. Implement backup/disaster recovery
4. Scale based on usage patterns