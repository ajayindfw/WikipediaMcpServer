#!/usr/bin/env node

/**
 * MCP HTTP Bridge Script for Wikipedia MCP Server
 * 
 * This script bridges MCP JSON-RPC communication over stdio to HTTP requests.
 * It reads MCP messages from stdin, converts them to HTTP POST requests,
 * and sends them to the remote Wikipedia MCP Server on supported platforms.
 * 
 * Version: 1.1.0 - Added logging/setLevel method support for VS Code MCP extension compatibility
 * 
 * Supported Platforms:
 *   - render: https://wikipediamcpserver.onrender.com/mcp/rpc
 *   - railway: https://wikipedia-mcp-server-production.up.railway.app/mcp/rpc
 * 
 * Local Method Handling:
 *   - logging/setLevel: Handled locally to prevent VS Code MCP errors
 *   - notifications/initialized: Handled locally for proper initialization
 * 
 * Usage:
 *   node mcp-http-bridge.js [provider]
 *   
 * Examples:
 *   node mcp-http-bridge.js render   # Connect to Render deployment
 *   node mcp-http-bridge.js railway  # Connect to Railway deployment
 *   node mcp-http-bridge.js          # Default to Render
 */

const https = require('https');
const http = require('http');
const readline = require('readline');

// Platform configurations
const PLATFORMS = {
    render: {
        url: 'https://wikipediamcpserver.onrender.com/mcp/rpc',
        name: 'Render'
    },
    railway: {
        url: 'https://wikipedia-mcp-server-production.up.railway.app/mcp/rpc',
        name: 'Railway'
    }
};

// Get provider from command line argument or environment variable
const provider = process.argv[2] || process.env.MCP_PROVIDER || 'render';

// Handle help requests
if (provider === '--help' || provider === '-h' || provider === 'help') {
    console.log(`
MCP HTTP Bridge for Wikipedia MCP Server (v1.1.0)

Usage: node mcp-http-bridge.js [provider]

Supported providers:
  render   - Connect to Render deployment (default)
             URL: https://wikipediamcpserver.onrender.com/mcp/rpc
  railway  - Connect to Railway deployment  
             URL: https://wikipedia-mcp-server-production.up.railway.app/mcp/rpc

Environment variables:
  MCP_PROVIDER        - Set default provider (render|railway)
  REMOTE_SERVER_URL   - Override the server URL
  MCP_DEBUG          - Enable debug logging (true|false)

Features:
  ✅ Multi-platform deployment support
  ✅ VS Code MCP extension compatibility (logging/setLevel support)
  ✅ Automatic error handling and timeouts
  ✅ Debug logging and troubleshooting

Examples:
  node mcp-http-bridge.js render
  node mcp-http-bridge.js railway
  MCP_PROVIDER=railway node mcp-http-bridge.js
  REMOTE_SERVER_URL=https://custom.com/mcp/rpc node mcp-http-bridge.js
`);
    process.exit(0);
}

const platformConfig = PLATFORMS[provider];

if (!platformConfig) {
    console.error(`Error: Unsupported provider '${provider}'. Supported providers: ${Object.keys(PLATFORMS).join(', ')}`);
    console.error(`Use 'node mcp-http-bridge.js --help' for usage information.`);
    process.exit(1);
}

// Configuration - can be overridden with environment variable
const REMOTE_SERVER_URL = process.env.REMOTE_SERVER_URL || platformConfig.url;
const TIMEOUT = 30000; // 30 seconds
const DEBUG = process.env.MCP_DEBUG === 'true';

/**
 * Log debug messages to stderr
 */
function debug(message) {
    if (DEBUG) {
        console.error(`[MCP-Bridge DEBUG] ${message}`);
    }
}

/**
 * Send HTTP POST request to remote server
 */
function sendHttpRequest(data) {
    return new Promise((resolve, reject) => {
        const postData = JSON.stringify(data);
        
        debug(`Sending request to ${REMOTE_SERVER_URL}`);
        debug(`Request data: ${postData}`);
        
        const url = new URL(REMOTE_SERVER_URL);
        const isHttps = url.protocol === 'https:';
        
        const options = {
            hostname: url.hostname,
            port: url.port || (isHttps ? 443 : 80),
            path: url.pathname,
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Content-Length': Buffer.byteLength(postData),
                'User-Agent': 'MCP-HTTP-Bridge/1.0'
            }
        };

        const protocol = isHttps ? https : http;
        const req = protocol.request(options, (res) => {
            let responseData = '';
            
            res.on('data', (chunk) => {
                responseData += chunk;
            });
            
            res.on('end', () => {
                debug(`Response status: ${res.statusCode}`);
                debug(`Response data: ${responseData}`);
                
                if (res.statusCode !== 200) {
                    reject(new Error(`HTTP ${res.statusCode}: ${responseData}`));
                    return;
                }
                
                try {
                    const jsonResponse = JSON.parse(responseData);
                    resolve(jsonResponse);
                } catch (parseError) {
                    debug(`JSON parse error: ${parseError.message}`);
                    reject(new Error(`Invalid JSON response: ${parseError.message}`));
                }
            });
        });

        req.on('error', (error) => {
            debug(`Request error: ${error.message}`);
            reject(error);
        });

        req.setTimeout(TIMEOUT, () => {
            debug('Request timeout');
            req.destroy();
            reject(new Error('Request timeout'));
        });

        req.write(postData);
        req.end();
    });
}

/**
 * Create MCP error response
 */
function createErrorResponse(id, code, message, data = null) {
    return {
        jsonrpc: "2.0",
        id: id,
        error: {
            code: code,
            message: message,
            data: data
        }
    };
}

/**
 * Process MCP message
 */
async function processMcpMessage(message) {
    try {
        debug(`Processing message: ${JSON.stringify(message)}`);
        
        // Validate JSON-RPC format
        if (!message.jsonrpc || message.jsonrpc !== "2.0") {
            return createErrorResponse(message.id || null, -32600, "Invalid Request");
        }
        
        if (!message.method) {
            return createErrorResponse(message.id || null, -32600, "Missing method");
        }

        // Handle logging methods locally (VS Code MCP extension expects these)
        if (message.method === 'logging/setLevel') {
            debug(`Logging setLevel called with: ${JSON.stringify(message.params)}`);
            return {
                jsonrpc: "2.0",
                id: message.id,
                result: {} // Empty result indicates success
            };
        }

        if (message.method === 'notifications/initialized') {
            debug(`Notifications initialized`);
            return {
                jsonrpc: "2.0",
                id: message.id,
                result: {} // Empty result indicates success
            };
        }

        // Forward all other methods to remote server
        const response = await sendHttpRequest(message);
        debug(`Received response: ${JSON.stringify(response)}`);
        
        return response;
        
    } catch (error) {
        debug(`Error processing message: ${error.message}`);
        return createErrorResponse(
            message.id || null,
            -32603,
            "Internal error",
            error.message
        );
    }
}

/**
 * Main function - process stdin line by line
 */
async function main() {
    debug(`MCP HTTP Bridge starting for ${platformConfig.name}...`);
    debug(`Provider: ${provider}`);
    debug(`Remote server: ${REMOTE_SERVER_URL}`);
    debug(`Timeout: ${TIMEOUT}ms`);
    
    const rl = readline.createInterface({
        input: process.stdin,
        crlfDelay: Infinity
    });

    rl.on('line', async (line) => {
        const trimmedLine = line.trim();
        if (!trimmedLine) {
            return; // Skip empty lines
        }

        try {
            debug(`Received line: ${trimmedLine}`);
            const message = JSON.parse(trimmedLine);
            const response = await processMcpMessage(message);
            
            // Output response to stdout
            console.log(JSON.stringify(response));
            
        } catch (parseError) {
            debug(`JSON parse error: ${parseError.message}`);
            const errorResponse = createErrorResponse(null, -32700, "Parse error");
            console.log(JSON.stringify(errorResponse));
        }
    });

    rl.on('close', () => {
        debug('Input stream closed, exiting...');
        // Give a small delay to ensure any pending responses are flushed
        setTimeout(() => {
            process.exit(0);
        }, 100);
    });

    process.on('SIGINT', () => {
        debug('Received SIGINT, exiting...');
        process.exit(0);
    });

    process.on('SIGTERM', () => {
        debug('Received SIGTERM, exiting...');
        process.exit(0);
    });
}

// Start the bridge
main().catch((error) => {
    console.error(`Fatal error: ${error.message}`);
    process.exit(1);
});