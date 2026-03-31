<!-- mcp-name: io.github.pierre3/zap-mcp -->
# dotnet-zap-mcp

[![NuGet](https://img.shields.io/nuget/v/dotnet-zap-mcp.svg)](https://www.nuget.org/packages/dotnet-zap-mcp)
[![NuGet Downloads](https://img.shields.io/nuget/dt/dotnet-zap-mcp.svg)](https://www.nuget.org/packages/dotnet-zap-mcp)
[![Release](https://github.com/pierre3/dotnet-zap-mcp/actions/workflows/release.yml/badge.svg)](https://github.com/pierre3/dotnet-zap-mcp/actions/workflows/release.yml)

MCP (Model Context Protocol) server for [OWASP ZAP](https://www.zaproxy.org/). Enables AI agents (Claude, GitHub Copilot, etc.) to drive ZAP vulnerability scanning via MCP.

## Features

- 45 MCP tools for controlling OWASP ZAP (scanning, alerts, spider, ajax spider, context, authentication, reports, etc.)
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

#### Data persistence

The ZAP container uses two Docker named volumes:

| Volume | Container Path | Purpose |
|--------|---------------|----------|
| `zap-home` | `/home/zap/.ZAP` | ZAP settings, contexts, sessions, scan policies (persisted across restarts) |
| `zap-data` | `/zap/wrk/data` | Shared directory for reports, session files, and context import/export |

On the first launch, the template `config.xml` is copied into `zap-home`. On subsequent launches, only the API key is updated — any changes made through ZAP (contexts, authentication settings, scan policies, etc.) are preserved.

The `zap-data` volume contains:
- `reports/` — generated scan reports
- `sessions/` — saved ZAP sessions
- `contexts/` — exported context files

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

| Tool | Parameters | Description |
|------|-----------|-------------|
| `DockerComposeUp` | — | Start the ZAP container and wait for healthy |
| `DockerComposeDown` | — | Stop and remove the ZAP container |
| `DockerComposeStatus` | — | Check container status |
| `DockerComposeLogs` | `tail` | Get recent container logs |

### ZAP Core

| Tool | Parameters | Description |
|------|-----------|-------------|
| `GetVersion` | — | Verify ZAP connectivity |
| `GetHosts` | — | List recorded hosts |
| `GetSites` | — | List recorded sites |
| `GetUrls` | `baseUrl` | List recorded URLs for a base URL |

### Spider

| Tool | Parameters | Description |
|------|-----------|-------------|
| `StartSpider` | `url`, `maxChildren`, `recurse`, `subtreeOnly`, `contextName` | Start a spider scan to crawl and discover pages |
| `GetSpiderStatus` | `scanId` | Check spider progress (0-100%) |
| `GetSpiderResults` | `scanId` | Get URLs discovered by spider |
| `StopSpider` | `scanId` | Stop a running spider scan |

### Active Scan

| Tool | Parameters | Description |
|------|-----------|-------------|
| `StartActiveScan` | `url`, `recurse`, `inScopeOnly`, `scanPolicyName`, `contextId` | Start an active vulnerability scan |
| `GetActiveScanStatus` | `scanId` | Check active scan progress (0-100%) |
| `StopActiveScan` | `scanId` | Stop a running active scan |

### Passive Scan

| Tool | Parameters | Description |
|------|-----------|-------------|
| `GetPassiveScanStatus` | — | Check passive scan progress (records remaining) |

### Alerts

| Tool | Parameters | Description |
|------|-----------|-------------|
| `GetAlertsSummary` | `baseUrl` | Alert counts by risk level |
| `GetAlerts` | `baseUrl`, `start`, `count`, `riskId` | Detailed alert list with pagination and risk filter |

### Reports

| Tool | Parameters | Description |
|------|-----------|-------------|
| `GetHtmlReport` | — | Generate HTML scan report |
| `GetJsonReport` | — | Generate JSON scan report |
| `GetXmlReport` | — | Generate XML scan report |

### Context Management

| Tool | Parameters | Description |
|------|-----------|-------------|
| `GetContextList` | — | List all contexts defined in ZAP |
| `GetContext` | `contextName` | Get context details (scope patterns, etc.) |
| `CreateContext` | `contextName` | Create a new context |
| `RemoveContext` | `contextName` | Delete a context |
| `IncludeInContext` | `contextName`, `regex` | Add URL include pattern to context scope |
| `ExcludeFromContext` | `contextName`, `regex` | Add URL exclude pattern to context scope |
| `ImportContext` | `contextFilePath` | Import a context file into ZAP |
| `ExportContext` | `contextName`, `contextFilePath` | Export a context to file |

### Authentication

| Tool | Parameters | Description |
|------|-----------|-------------|
| `GetAuthenticationMethod` | `contextId` | Get the authentication method configured for a context |
| `SetAuthenticationMethod` | `contextId`, `authMethodName`, `authMethodConfigParams` | Set authentication method (form-based, JSON-based, script-based, HTTP) |
| `SetLoggedInIndicator` | `contextId`, `loggedInIndicatorRegex` | Set regex pattern indicating logged-in state |
| `SetLoggedOutIndicator` | `contextId`, `loggedOutIndicatorRegex` | Set regex pattern indicating logged-out state |

### Users

| Tool | Parameters | Description |
|------|-----------|-------------|
| `GetUsersList` | `contextId` | List all users for a context |
| `CreateUser` | `contextId`, `name` | Create a new user |
| `RemoveUser` | `contextId`, `userId` | Remove a user |
| `SetAuthenticationCredentials` | `contextId`, `userId`, `authCredentialsConfigParams` | Set user credentials (username/password) |
| `SetUserEnabled` | `contextId`, `userId`, `enabled` | Enable or disable a user |

### Forced User

| Tool | Parameters | Description |
|------|-----------|-------------|
| `SetForcedUser` | `contextId`, `userId` | Set the forced user for a context |
| `SetForcedUserModeEnabled` | `enabled` | Enable or disable forced user mode globally |
| `GetForcedUserStatus` | `contextId` | Get forced user mode status and current forced user |

### Ajax Spider

| Tool | Parameters | Description |
|------|-----------|-------------|
| `StartAjaxSpider` | `url`, `inScope`, `contextName`, `subtreeOnly` | Start the Ajax Spider for JavaScript-heavy apps |
| `StartAjaxSpiderAsUser` | `contextName`, `userId`, `url`, `subtreeOnly` | Start the Ajax Spider as a specific user |
| `GetAjaxSpiderStatus` | — | Get Ajax Spider status (running/stopped) |
| `GetAjaxSpiderResults` | — | Get Ajax Spider results summary |
| `StopAjaxSpider` | — | Stop the Ajax Spider |

## Typical Workflow

1. Agent calls `DockerComposeUp` to start ZAP
2. Configure browser/Playwright to use ZAP as proxy (`http://127.0.0.1:8090`)
3. Browse the target application through the proxy
4. Agent calls `StartSpider` to crawl the application, then `GetSpiderStatus` to monitor progress
5. Agent calls `GetPassiveScanStatus` to wait for passive scan completion
6. Agent calls `StartActiveScan` on key pages, then `GetActiveScanStatus` to monitor progress
7. Agent calls `GetAlertsSummary` and `GetAlerts` to retrieve vulnerability findings
8. Agent calls `GetHtmlReport` or `GetJsonReport` to generate a scan report
9. Agent calls `DockerComposeDown` when done

> **Note:** ZAP settings (contexts, authentication, scan policies) are persisted in the `zap-home` Docker volume. You can stop and restart the container without losing configuration.

## Try It Out (with the included vulnerable app)

This repository includes an intentionally vulnerable web application for trying out ZAP scanning. The app contains common vulnerabilities (XSS, SQL Injection, CSRF, Open Redirect) so ZAP can detect real findings.

### Setup

```bash
# Start ZAP and the vulnerable target app
docker compose -f tests/docker/docker-compose.test.yml up -d --build

# Wait until both containers are healthy
docker compose -f tests/docker/docker-compose.test.yml ps
```

Then configure your MCP client to connect to this ZAP instance:

```json
{
  "mcpServers": {
    "zap": {
      "command": "zap-mcp",
      "env": {
        "ZAP_BASE_URL": "http://localhost:8090",
        "ZAP_API_KEY": "test-api-key-for-ci"
      }
    }
  }
}
```

> The vulnerable app is accessible at `http://target` from within the Docker network (used by ZAP), and at `http://localhost:8080` from your host machine.

### Example 1: Quick Scan

**Prompt to agent:**

> Scan http://target for vulnerabilities. Run a spider crawl, wait for the passive scan to finish, then show me the alert summary and generate an HTML report.

**Expected tool flow:**

```
GetVersion          → Verify ZAP connectivity
StartSpider         → url: "http://target"
GetSpiderStatus     → Poll until 100%
GetPassiveScanStatus → Poll until 0 records remaining
GetAlertsSummary    → baseUrl: "http://target"
GetAlerts           → baseUrl: "http://target"
GetHtmlReport       → Generate report
```

ZAP will discover pages like `/search`, `/login`, `/users`, `/about`, `/contact`, and the passive scan will report issues such as missing security headers and CSRF vulnerabilities.

### Example 2: Authenticated Scan

The `/admin` page requires login (username: `admin`, password: `password`). An authenticated scan lets ZAP access protected pages.

**Prompt to agent:**

> Set up an authenticated scan for http://target. The login form is at /login with fields "username" and "password" (credentials: admin / password). The logged-in indicator is "Welcome, admin". After configuring authentication, spider the site as the authenticated user and run an active scan.

**Expected tool flow:**

```
CreateContext                   → contextName: "target-auth"
IncludeInContext                → regex: "http://target.*"
SetAuthenticationMethod         → contextId, authMethodName: "formBasedAuthentication",
                                  authMethodConfigParams: "loginUrl=http://target/login&loginRequestData=username%3D%7B%25username%25%7D%26password%3D%7B%25password%25%7D"
SetLoggedInIndicator            → loggedInIndicatorRegex: "Welcome, admin"
CreateUser                      → contextId, name: "admin"
SetAuthenticationCredentials    → contextId, userId, authCredentialsConfigParams: "username=admin&password=password"
SetUserEnabled                  → contextId, userId, enabled: true
SetForcedUser                   → contextId, userId
SetForcedUserModeEnabled        → enabled: true
StartSpider                     → url: "http://target", contextName: "target-auth"
GetSpiderStatus                 → Poll until 100%
GetPassiveScanStatus            → Poll until 0 records remaining
StartActiveScan                 → url: "http://target"
GetActiveScanStatus             → Poll until 100%
GetAlertsSummary                → baseUrl: "http://target"
GetAlerts                       → baseUrl: "http://target"
SetForcedUserModeEnabled        → enabled: false
```

With authentication configured, ZAP can access `/admin` and test for vulnerabilities behind the login.

### Example 3: Full Vulnerability Assessment

**Prompt to agent:**

> Perform a full vulnerability assessment of http://target. Crawl the site, run an active scan, then give me a detailed breakdown of all discovered vulnerabilities grouped by risk level.

**Expected tool flow:**

```
StartSpider          → url: "http://target", recurse: true
GetSpiderStatus      → Poll until 100%
GetSpiderResults     → Review discovered URLs
GetPassiveScanStatus → Poll until 0 records remaining
StartActiveScan      → url: "http://target", recurse: true
GetActiveScanStatus  → Poll until 100%
GetAlertsSummary     → baseUrl: "http://target"
GetAlerts            → baseUrl: "http://target", riskId: "3" (High)
GetAlerts            → baseUrl: "http://target", riskId: "2" (Medium)
GetAlerts            → baseUrl: "http://target", riskId: "1" (Low)
GetHtmlReport        → Generate final report
```

The active scan will detect vulnerabilities including:
- **High**: SQL Injection (`/users?id=`), Cross Site Scripting (`/search?q=`)
- **Medium**: CSRF (missing tokens on `/login`), Open Redirect (`/redirect?url=`)
- **Low/Informational**: Missing security headers, cookie issues, etc.

### Cleanup

```bash
docker compose -f tests/docker/docker-compose.test.yml down -v
```

## License

[MIT](LICENSE)
