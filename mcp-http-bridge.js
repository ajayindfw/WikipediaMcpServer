#!/usr/bin/env node

/**
 * MCP HTTP Bridge Script for Wikipedia MCP Server
 * 
 * This script bridges MCP JSON-RPC communication over stdio to HTTP requests.
 * It reads MCP messages from stdin, converts them to HTTP POST requests,
 * and sends them to the remote Wikipedia MCP Server on Render.
 * 
 * Usage:
 *   echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{...}}' | node mcp-http-bridge.js
 */

const https = require('https');
const readline = require('readline');

// Configuration
const REMOTE_SERVER_URL = 'https://wikipediamcpserver.onrender.com/api/wikipedia';
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
        
        const options = {
            hostname: url.hostname,
            port: url.port || 443,
            path: url.pathname,
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Content-Length': Buffer.byteLength(postData),
                'User-Agent': 'MCP-HTTP-Bridge/1.0'
            }
        };

        const req = https.request(options, (res) => {
            let responseData = '';
            
            res.on('data', (chunk) => {
                responseData += chunk;
            });
            
            res.on('end', () => {
                debug(`Response status: ${res.statusCode}`);
                debug(`Response data: ${responseData}`);
                
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

        // Send to remote server
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
    debug('MCP HTTP Bridge starting...');
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
        process.exit(0);
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