# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-03-31

### Added

- **Test suite**: 82 unit tests and 21 integration tests covering all 45 MCP tools
  - `ZapApiClientTests`: HTTP request construction and response parsing (27 tests)
  - `ZapToolsTests`: Output formatting for all MCP tool methods (55 tests)
  - Integration tests running against a real ZAP container and vulnerable target app (21 tests)
- **Vulnerable test web application**: Flask-based app with intentional vulnerabilities (XSS, SQL Injection, CSRF, Open Redirect) for integration testing and demonstration
- **Docker Compose test environment** (`tests/docker/docker-compose.test.yml`): ZAP + vulnerable target app with health checks
- **CI pipeline** (`.github/workflows/ci.yml`): Runs unit tests, starts Docker containers, runs integration tests
- **"Try It Out" section in README**: Three step-by-step usage examples with the included vulnerable app
  - Example 1: Quick Scan (Spider + Passive Scan + Alerts)
  - Example 2: Authenticated Scan (Context + Form-based Auth + Forced User + Active Scan)
  - Example 3: Full Vulnerability Assessment (Active Scan + Risk-level filtering + Report)
- `tests/README.md`: Detailed test documentation with full test method listing

### Fixed

- **ZAP 2.17.0 compatibility**: `GetContextListAsync` now handles both JSON array format (ZAP >= 2.17.0) and legacy string format
- **Docker volume permissions**: Containers now start as root to fix volume ownership via `chown`, then drop to `zap` user via `setpriv`. Resolves `NO_ACCESS` errors on context export and other file operations
- **Docker Compose configuration**: Added `user: "0:0"` to both production and test `docker-compose.yml` for correct volume permission handling

### Changed

- `entrypoint.sh`: Reordered to create directories first, then fix ownership and drop privileges
- `dotnet-zap-mcp.csproj`: Added `InternalsVisibleTo` for test project access
- `ZapApiClient.cs`: Added internal constructor accepting `HttpClient` for unit testing
- Updated Docker prerequisites description in README ("Docker Desktop" → "Docker Engine or Docker Desktop with `docker compose` support")

## [1.0.2] - 2026-03-29

### Added

- Initial public release with 45 MCP tools
- Built-in Docker Compose management
- Zero-config setup with auto-generated API keys
- MCP Registry publishing in release workflow
