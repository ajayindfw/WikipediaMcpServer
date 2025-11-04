#!/usr/bin/env python3
"""
Wikipedia MCP Server - Local Testing Startup Script
This script starts the Wikipedia MCP Server for local testing with Postman or other tools
"""

import os
import sys
import subprocess
import signal
import time
import platform

def print_banner():
    print("🚀 Starting Wikipedia MCP Server for Local Testing")
    print("=" * 50)

def check_dotnet():
    """Check if .NET SDK is available and using correct version"""
    try:
        result = subprocess.run(['dotnet', '--version'], 
                              capture_output=True, text=True, check=True)
        version = result.stdout.strip()
        print(f"✅ .NET SDK found: {version}")
        
        # Check if using .NET 8 (due to global.json)
        if not version.startswith('8.'):
            print(f"⚠️  Warning: Using .NET {version} instead of .NET 8")
            print("   global.json should pin to .NET 8.0.406")
        
        return True
    except (subprocess.CalledProcessError, FileNotFoundError):
        print("❌ .NET SDK not found. Please install .NET 8.0 SDK")
        print("   Download from: https://dotnet.microsoft.com/download")
        return False

def check_project():
    """Check if project file exists"""
    project_path = "src/WikipediaMcpServer/WikipediaMcpServer.csproj"
    if not os.path.exists(project_path):
        print("❌ Project file not found. Make sure you're in the correct directory.")
        return False
    return True

def kill_existing_processes():
    """Kill any existing server instances"""
    print("🧹 Cleaning up any existing server instances...")
    
    if platform.system() == "Windows":
        try:
            subprocess.run(['taskkill', '/f', '/im', 'dotnet.exe'], 
                         capture_output=True, check=False)
        except:
            pass
    else:
        try:
            subprocess.run(['pkill', '-f', 'dotnet.*WikipediaMcpServer'], 
                         capture_output=True, check=False)
        except:
            pass
    
    time.sleep(2)

def build_project():
    """Build the project"""
    print("🔨 Building the project...")
    try:
        result = subprocess.run(['dotnet', 'build', 
                               'src/WikipediaMcpServer/WikipediaMcpServer.csproj'], 
                              check=True)
        print("✅ Build successful!")
        return True
    except subprocess.CalledProcessError:
        print("❌ Build failed. Please check the build errors above.")
        return False

def print_server_info():
    """Print server information"""
    print("\n🌐 Starting server on http://localhost:5070")
    print("📝 Server logs will be displayed below...")
    print("⏹️  Press Ctrl+C to stop the server")
    print("\nAvailable endpoints:")
    print("  🏥 Health: http://localhost:5070/health")
    print("  ℹ️  Info:   http://localhost:5070/info")
    print("  📋 Swagger: http://localhost:5070/swagger")
    print("  🔗 MCP JSON-RPC: http://localhost:5070/mcp/rpc")
    print("  🔗 MCP SDK: http://localhost:5070/mcp")
    print("\nReady for Postman testing! 🚀")
    print("Use WikipediaMcpServer-MCP-JsonRPC-Collection.json for comprehensive testing")
    print()

def start_server():
    """Start the server"""
    try:
        subprocess.run(['dotnet', 'run', '--project', 
                       'src/WikipediaMcpServer/WikipediaMcpServer.csproj'], 
                      check=True)
    except KeyboardInterrupt:
        print("\n🛑 Server stopped by user")
    except subprocess.CalledProcessError as e:
        print(f"❌ Server failed to start: {e}")

def main():
    # Change to script directory
    script_dir = os.path.dirname(os.path.abspath(__file__))
    os.chdir(script_dir)
    
    print_banner()
    
    # Check prerequisites
    if not check_dotnet():
        sys.exit(1)
    
    if not check_project():
        sys.exit(1)
    
    # Setup
    kill_existing_processes()
    
    if not build_project():
        sys.exit(1)
    
    print_server_info()
    
    # Start server
    start_server()

if __name__ == "__main__":
    main()