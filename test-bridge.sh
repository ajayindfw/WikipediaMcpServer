#!/bin/bash

echo "Testing MCP Bridge Connection..."
echo

# Test 1: Initialize
echo "1. Testing initialize..."
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{}}}' | node mcp-http-bridge.js 2>/dev/null

echo
echo "2. Testing tools/list..."
echo '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' | node mcp-http-bridge.js 2>/dev/null

echo 
echo "3. Testing search tool..."
echo '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"wikipedia_search","arguments":{"query":"python"}}}' | node mcp-http-bridge.js 2>/dev/null

echo
echo "Test completed!"