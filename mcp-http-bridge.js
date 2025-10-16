#!/usr/bin/env node

const readline = require('readline');
const https = require('https');

// Ensure line buffering for proper MCP communication
process.stdout.setDefaultEncoding('utf8');
process.stderr.setDefaultEncoding('utf8');

const rl = readline.createInterface({
  input: process.stdin,
  output: false, // Don't echo to stdout
  terminal: false
});

// Log to stderr for debugging (VS Code MCP extension will show this)
function debugLog(message) {
  process.stderr.write(`[MCP-Bridge] ${message}\n`);
}

debugLog('MCP HTTP Bridge started for Wikipedia server');

rl.on('line', (line) => {
  if (!line.trim()) return;
  
  debugLog(`Received: ${line.substring(0, 100)}...`);
  
  try {
    const request = JSON.parse(line);
    const data = JSON.stringify(request);
    
    const options = {
      hostname: 'wikipediamcpserver.onrender.com',
      port: 443,
      path: '/api/wikipedia',
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Content-Length': Buffer.byteLength(data)
      },
      timeout: 10000 // 10 second timeout
    };

    debugLog(`Sending request to: https://${options.hostname}${options.path}`);

    const req = https.request(options, (res) => {
      let responseData = '';
      res.on('data', (chunk) => {
        responseData += chunk;
      });
      res.on('end', () => {
        debugLog(`Received response: ${responseData.substring(0, 100)}...`);
        // Write response to stdout with proper line ending
        process.stdout.write(responseData + '\n');
        process.stdout.uncork();
      });
    });

    req.on('timeout', () => {
      debugLog('Request timeout');
      req.destroy();
      const errorResponse = JSON.stringify({
        jsonrpc: "2.0",
        id: request.id || null,
        error: {
          code: -32603,
          message: "Request timeout"
        }
      });
      process.stdout.write(errorResponse + '\n');
      process.stdout.uncork();
    });

    req.on('error', (e) => {
      debugLog(`Request error: ${e.message}`);
      const errorResponse = JSON.stringify({
        jsonrpc: "2.0",
        id: request.id || null,
        error: {
          code: -32603,
          message: `Network error: ${e.message}`
        }
      });
      process.stdout.write(errorResponse + '\n');
      process.stdout.uncork();
    });

    req.write(data);
    req.end();
  } catch (e) {
    debugLog(`Parse error: ${e.message}`);
    const errorResponse = JSON.stringify({
      jsonrpc: "2.0", 
      id: null,
      error: {
        code: -32700,
        message: `Parse error: ${e.message}`
      }
    });
    process.stdout.write(errorResponse + '\n');
    process.stdout.uncork();
  }
});

rl.on('close', () => {
  debugLog('MCP Bridge closing');
  process.exit(0);
});

process.on('SIGINT', () => {
  debugLog('MCP Bridge interrupted');
  process.exit(0);
});

process.on('SIGTERM', () => {
  debugLog('MCP Bridge terminated');
  process.exit(0);
});