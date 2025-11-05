#!/bin/bash

# Demo Master Script: Quick switcher for Wikipedia MCP servers
echo "🎯 Wikipedia MCP Server Demo Switcher"
echo "====================================="
echo ""
echo "Choose your demonstration server:"
echo ""
echo "1. 🔥 Local Development (with SSE streaming demos)"
echo "2. 🌐 Render Cloud Deployment"  
echo "3. 🚂 Railway Cloud Deployment"
echo "4. 📊 Show Current Status"
echo "5. 🚪 Exit"
echo ""

read -p "Enter your choice (1-5): " choice

case $choice in
    1)
        echo ""
        ./demo-switch-to-local.sh
        ;;
    2)
        echo ""
        ./demo-switch-to-render.sh
        ;;
    3)
        echo ""
        ./demo-switch-to-railway.sh
        ;;
    4)
        echo ""
        ./demo-status.sh
        ;;
    5)
        echo "👋 Demo preparation complete!"
        exit 0
        ;;
    *)
        echo "❌ Invalid choice. Please run the script again."
        exit 1
        ;;
esac

echo ""
echo "🎪 Ready for your demo presentation!"