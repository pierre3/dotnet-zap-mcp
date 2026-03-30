<!-- mcp-name: io.github.pierre3/zap-mcp -->
# dotnet-zap-mcp

[![NuGet](https://img.shields.io/nuget/v/dotnet-zap-mcp.svg)](https://www.nuget.org/packages/dotnet-zap-mcp)
[![NuGet Downloads](https://img.shields.io/nuget/dt/dotnet-zap-mcp.svg)](https://www.nuget.org/packages/dotnet-zap-mcp)
[![Release](https://github.com/pierre3/dotnet-zap-mcp/actions/workflows/release.yml/badge.svg)](https://github.com/pierre3/dotnet-zap-mcp/actions/workflows/release.yml)

MCP (Model Context Protocol) server for [OWASP ZAP](https://www.zaproxy.org/). Enables AI agents (Claude, GitHub Copilot, etc.) to drive ZAP vulnerability scanning via MCP.

## Features

- 14 MCP tools for controlling OWASP ZAP (scanning, alerts, spider, etc.)
- Built-in Docker Compose management (start/stop ZAP with a single tool call)
- Zero-config setup: auto-generates API keys and extracts Docker assets
- Works with any MCP-compatible client (Claude Desktop, VS Code, etc.)

## Installation

```bash
dotnet tool install -g dotnet-zap-mcp
```

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker (Docker Engine or Docker Desktop) with `docker compose` support (for built-in ZAP container management)

## Configuration

### Zero-config (recommended)

No configuration needed. The agent calls `DockerComposeUp` which automatically:
1. Extracts Docker assets to `~/.zap-mcp/docker/`
2. Generates a random API key
3. Starts the ZAP container on `localhost:8090`
4. Waits for ZAP to become healthy

### Claude Desktop / Claude Code

Add to your MCP configuration:

```json
{
  "mcpServers": {
    "zap": {
      "command": "zap-mcp"
    }
  }
}
```

### VS Code (Copilot)

Add to `.vscode/mcp.json`:

```json
{
  "servers": {
    "zap": {
      "command": "zap-mcp"
    }
  }
}
```

### With existing ZAP instance

If you already have a ZAP instance running, pass connection details via environment variables:

```json
{
  "mcpServers": {
    "zap": {
      "command": "zap-mcp",
      "env": {
        "ZAP_BASE_URL": "http://localhost:8090",
        "ZAP_API_KEY": "your-api-key"
      }
    }
  }
}
```

## Available Tools

### Docker Management

| Tool | Description |
|------|-------------|
| `DockerComposeUp` | Start the ZAP container and wait for healthy |
| `DockerComposeDown` | Stop and remove the ZAP container |
| `DockerComposeStatus` | Check container status |
| `DockerComposeLogs` | Get recent container logs |

### ZAP Scanning

| Tool | Description |
|------|-------------|
| `GetVersion` | Verify ZAP connectivity |
| `GetHosts` | List recorded hosts |
| `GetSites` | List recorded sites |
| `GetUrls` | List recorded URLs for a base URL |
| `GetPassiveScanStatus` | Check passive scan progress |
| `GetAlertsSummary` | Alert counts by risk level |
| `GetAlerts` | Detailed alert list with pagination |
| `StartSpider` | Start a spider scan |
| `GetSpiderStatus` | Check spider progress |
| `StartActiveScan` | Start an active vulnerability scan |
| `GetActiveScanStatus` | Check active scan progress |
| `StopActiveScan` | Stop a running active scan |

## Typical Workflow

1. Agent calls `DockerComposeUp` to start ZAP
2. Configure browser/Playwright to use ZAP as proxy (`http://127.0.0.1:8090`)
3. Browse the target application through the proxy
4. Agent calls `GetPassiveScanStatus` to wait for passive scan completion
5. Agent calls `StartActiveScan` on key pages
6. Agent calls `GetAlerts` to retrieve vulnerability findings
7. Agent calls `DockerComposeDown` when done

## License

[MIT](LICENSE)
