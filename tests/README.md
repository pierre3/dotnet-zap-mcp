# Tests

The dotnet-zap-mcp test suite consists of two categories: unit tests and integration tests.

## Structure

```
tests/
├── dotnet-zap-mcp.Tests/
│   ├── Helpers/
│   │   └── MockHttpMessageHandler.cs   # HTTP mock handler
│   ├── IntegrationTests/
│   │   ├── ZapIntegrationFixture.cs    # Docker environment fixture
│   │   ├── ZapCoreIntegrationTests.cs
│   │   ├── ZapContextIntegrationTests.cs
│   │   ├── ZapSpiderIntegrationTests.cs
│   │   ├── ZapActiveScanIntegrationTests.cs
│   │   ├── ZapAlertsIntegrationTests.cs
│   │   └── ZapAjaxSpiderIntegrationTests.cs
│   ├── ZapApiClientTests.cs            # API client unit tests
│   └── ZapToolsTests.cs               # Tool output formatting unit tests
└── docker/
    ├── docker-compose.test.yml         # Test Docker Compose
    └── vulnerable-app/
        ├── Dockerfile
        └── app.py                      # Intentionally vulnerable web app
```

## Running Tests

### Unit Tests

```bash
dotnet test tests/dotnet-zap-mcp.Tests --filter "Category!=Integration"
```

### Integration Tests

Docker containers must be running.

```bash
# 1. Start the test environment
docker compose -f tests/docker/docker-compose.test.yml up -d --build

# 2. Wait until ZAP is healthy
docker compose -f tests/docker/docker-compose.test.yml ps

# 3. Run integration tests
dotnet test tests/dotnet-zap-mcp.Tests --filter "Category=Integration"

# 4. Stop the test environment
docker compose -f tests/docker/docker-compose.test.yml down -v
```

---

## Unit Tests (82 tests)

### ZapApiClientTests (27 tests)

Tests HTTP request construction and response parsing of `ZapApiClient` using a mock HTTP handler.

| Test Method | Verifies |
|---|---|
| `GetVersionAsync_AppendsApiKey` | API key is appended to request URLs |
| `GetVersionAsync_ParsesVersion` | Version string is correctly parsed from JSON response |
| `GetHostsAsync_ReturnsHosts` | Hosts list is returned correctly |
| `GetHostsAsync_ReturnsEmpty` | Empty array is returned when no hosts exist |
| `GetSitesAsync_ReturnsSites` | Sites list is returned correctly |
| `GetUrlsAsync_EncodesBaseUrl` | Base URL is properly URL-encoded in requests |
| `GetAlertsSummaryAsync_ReturnsJson` | Alert summary JSON structure is returned |
| `GetAlertsAsync_PassesRiskIdFilter` | riskId filter parameter is passed correctly |
| `GetPassiveScanRecordsToScanAsync_ParsesInt` | Passive scan records count is parsed as integer |
| `StartSpiderAsync_ReturnsScanId` | Spider scan ID is returned |
| `GetSpiderStatusAsync_ParsesPercentage` | Spider progress percentage is parsed correctly |
| `GetSpiderResultsAsync_ReturnsResults` | Spider results are returned as JSON |
| `StartActiveScanAsync_ReturnsScanId` | Active scan ID is returned |
| `GetActiveScanStatusAsync_ParsesPercentage` | Active scan progress percentage is parsed correctly |
| `GetHtmlReportAsync_ReturnsBytes` | HTML report is returned as byte array |
| `GetJsonReportAsync_ReturnsBytes` | JSON report is returned as byte array |
| `GetContextListAsync_ParsesContextList` | Context list is parsed from both string and array formats |
| `GetContextListAsync_ReturnsEmpty` | Empty list is returned when no contexts exist |
| `CreateContextAsync_ReturnsContextId` | Context ID is returned on creation |
| `SetAuthenticationMethodAsync_SendsCorrectParams` | Authentication method parameters are sent correctly |
| `CreateUserAsync_ReturnsUserId` | User ID is returned on creation |
| `IsForcedUserModeEnabledAsync_ParsesBool` | Forced user mode enabled/disabled flag is parsed as boolean |
| `StartAjaxSpiderAsync_ReturnsResult` | Ajax Spider result is returned |
| `GetAjaxSpiderStatusAsync_ReturnsStatus` | Ajax Spider status is returned |
| `GetAjaxSpiderNumberOfResultsAsync_ParsesInt` | Ajax Spider result count is parsed as integer |
| `GetVersionAsync_ThrowsOnNon2xx` | `HttpRequestException` is thrown on non-2xx status |
| `Reconfigure_UsesNewBaseUrlAndApiKey` | New base URL and API key are used after `Reconfigure` call |

### ZapToolsTests (55 tests)

Tests the output formatting (user-facing strings) of each MCP tool method.

| Test Method | Target Tool | Verifies |
|---|---|---|
| `GetVersionAsync_FormatsOutput` | GetVersion | Output in `"ZAP version: X.XX.X"` format |
| `GetHostsAsync_WithHosts` | GetHosts | Hosts are listed in output |
| `GetHostsAsync_Empty` | GetHosts | `"No hosts recorded yet."` message |
| `GetSitesAsync_WithSites` | GetSites | Sites are listed in output |
| `GetSitesAsync_Empty` | GetSites | `"No sites recorded yet."` message |
| `GetUrlsAsync_WithUrls` | GetUrls | URL count and list in output |
| `GetUrlsAsync_Empty` | GetUrls | `"No URLs recorded for this base URL."` message |
| `GetPassiveScanStatusAsync_Complete` | GetPassiveScanStatus | Completion message with 0 remaining records |
| `GetPassiveScanStatusAsync_InProgress` | GetPassiveScanStatus | In-progress message with remaining count |
| `GetAlertsSummaryAsync_FormatsOutput` | GetAlertsSummary | Output includes High / Medium / Low risk levels |
| `GetAlertsAsync_FormatsAlerts` | GetAlerts | Output includes alert name, risk, confidence, URL, param, CWE ID |
| `GetAlertsAsync_NoAlerts` | GetAlerts | `"No alerts found."` message |
| `StartSpiderAsync_FormatsOutput` | StartSpider | `"Spider scan started. Scan ID: X"` format |
| `GetSpiderStatusAsync_Complete` | GetSpiderStatus | Completion message |
| `GetSpiderStatusAsync_InProgress` | GetSpiderStatus | In-progress message with percentage |
| `GetSpiderResultsAsync_WithResults` | GetSpiderResults | Discovered URL count in output |
| `GetSpiderResultsAsync_Empty` | GetSpiderResults | `"No URLs discovered yet."` message |
| `StopSpiderAsync_FormatsOutput` | StopSpider | `"Spider scan X stopped."` format |
| `StartActiveScanAsync_FormatsOutput` | StartActiveScan | `"Active scan started. Scan ID: X"` format |
| `GetActiveScanStatusAsync_Complete` | GetActiveScanStatus | Completion message |
| `GetActiveScanStatusAsync_InProgress` | GetActiveScanStatus | In-progress message with percentage |
| `StopActiveScanAsync_FormatsOutput` | StopActiveScan | `"Active scan X stopped."` format |
| `GetHtmlReportAsync_ReturnsString` | GetHtmlReport | HTML report returned as UTF-8 string |
| `GetJsonReportAsync_ReturnsString` | GetJsonReport | JSON report returned as string |
| `GetXmlReportAsync_ReturnsString` | GetXmlReport | XML report returned as string |
| `GetContextListAsync_WithContexts` | GetContextList | Context count and names in output |
| `GetContextListAsync_Empty` | GetContextList | `"No contexts defined."` message |
| `GetContextAsync_FormatsOutput` | GetContext | Context ID and description in output |
| `CreateContextAsync_FormatsOutput` | CreateContext | `"Context 'X' created. Context ID: Y"` format |
| `RemoveContextAsync_FormatsOutput` | RemoveContext | `"Context 'X' removed."` format |
| `IncludeInContextAsync_FormatsOutput` | IncludeInContext | Pattern and `"include list"` in output |
| `ExcludeFromContextAsync_FormatsOutput` | ExcludeFromContext | `"exclude list"` in output |
| `ImportContextAsync_FormatsOutput` | ImportContext | `"Context imported. Context ID: X"` format |
| `ExportContextAsync_FormatsOutput` | ExportContext | `"exported to"` in output |
| `GetAuthenticationMethodAsync_ReturnsJson` | GetAuthenticationMethod | Authentication method name in JSON output |
| `SetAuthenticationMethodAsync_FormatsOutput` | SetAuthenticationMethod | Method name and context ID in output |
| `SetLoggedInIndicatorAsync_FormatsOutput` | SetLoggedInIndicator | `"Logged-in indicator set"` message |
| `SetLoggedOutIndicatorAsync_FormatsOutput` | SetLoggedOutIndicator | `"Logged-out indicator set"` message |
| `GetUsersListAsync_WithUsers` | GetUsersList | User names and IDs in output |
| `GetUsersListAsync_Empty` | GetUsersList | `"No users configured for this context."` message |
| `CreateUserAsync_FormatsOutput` | CreateUser | `"User 'X' created. User ID: Y"` format |
| `RemoveUserAsync_FormatsOutput` | RemoveUser | `"User Y removed from context X."` format |
| `SetAuthenticationCredentialsAsync_FormatsOutput` | SetAuthenticationCredentials | `"Credentials set"` message |
| `SetUserEnabledAsync_Enable` | SetUserEnabled | `"enabled"` in output |
| `SetUserEnabledAsync_Disable` | SetUserEnabled | `"disabled"` in output |
| `SetForcedUserAsync_FormatsOutput` | SetForcedUser | `"Forced user set to X for context Y."` format |
| `SetForcedUserModeEnabledAsync_Enable` | SetForcedUserModeEnabled | `"Forced user mode enabled."` message |
| `GetForcedUserStatusAsync_FormatsOutput` | GetForcedUserStatus | Mode status and user ID in output (`"(none)"` when unset) |
| `StartAjaxSpiderAsync_FormatsOutput` | StartAjaxSpider | `"Ajax Spider started. Result: OK"` format |
| `StartAjaxSpiderAsUserAsync_FormatsOutput` | StartAjaxSpiderAsUser | `"as user X"` in output |
| `GetAjaxSpiderStatusAsync_Running` | GetAjaxSpiderStatus | `"Ajax Spider is running."` message |
| `GetAjaxSpiderStatusAsync_Stopped` | GetAjaxSpiderStatus | `"Ajax Spider status: stopped"` message |
| `GetAjaxSpiderResultsAsync_Zero` | GetAjaxSpiderResults | Zero results message |
| `GetAjaxSpiderResultsAsync_WithResults` | GetAjaxSpiderResults | Result count and request headers in output |
| `StopAjaxSpiderAsync_FormatsOutput` | StopAjaxSpider | `"Ajax Spider stopped."` message |

---

## Integration Tests (21 tests)

End-to-end tests that run against a real ZAP container and a vulnerable test web application.  
Identified by `[Trait("Category", "Integration")]` and selectable with `--filter "Category=Integration"`.

### ZapCoreIntegrationTests (7 tests)

| Test Method | Verifies |
|---|---|
| `GetVersion_ReturnsVersionString` | ZAP version string is non-empty |
| `GetHosts_ReturnsSuccessfully` | Hosts endpoint completes successfully |
| `GetSites_ReturnsSuccessfully` | Sites endpoint completes successfully |
| `GetPassiveScanRecordsToScan_ReturnsNonNegative` | Passive scan records count is >= 0 |
| `GetHtmlReport_ReturnsNonEmptyBytes` | HTML report byte length is > 0 |
| `GetJsonReport_ReturnsNonEmptyBytes` | JSON report byte length is > 0 |
| `GetXmlReport_ReturnsNonEmptyBytes` | XML report byte length is > 0 |

### ZapContextIntegrationTests (5 tests)

| Test Method | Verifies |
|---|---|
| `ContextCrud_CreateListGetRemove` | Full context lifecycle: create, list, get details, include/exclude patterns, remove |
| `ContextExportImport_RoundTrip` | Context export and import round-trip |
| `Authentication_SetAndGet` | Setting and retrieving authentication method and logged-in/logged-out indicators |
| `UserCrud_CreateGetSetRemove` | Full user lifecycle: create, list, set credentials, enable/disable, remove |
| `ForcedUser_SetAndGet` | Forced user configuration and mode enable/disable |

### ZapSpiderIntegrationTests (2 tests)

| Test Method | Verifies |
|---|---|
| `Spider_FullWorkflow` | Spider scan start, poll until 100%, results include `/search`, `/login`, etc. |
| `Spider_StopWhileRunning` | Running spider can be stopped |

### ZapActiveScanIntegrationTests (2 tests)

| Test Method | Verifies |
|---|---|
| `ActiveScan_StartAndMonitor` | Spider run, passive scan wait, active scan start, poll until 100% (5-minute timeout) |
| `ActiveScan_StopWhileRunning` | Running active scan can be stopped |

### ZapAlertsIntegrationTests (3 tests)

| Test Method | Verifies |
|---|---|
| `GetAlertsSummary_ReturnsValidStructure` | Alert summary contains risk level keys after spider + passive scan |
| `GetAlerts_ReturnsAlertList` | Alerts list is returned as an array |
| `GetAlerts_WithRiskIdFilter` | Filtered alert count is <= total alert count |

### ZapAjaxSpiderIntegrationTests (2 tests)

| Test Method | Verifies |
|---|---|
| `AjaxSpider_StartAndStop` | Ajax Spider start, status monitoring, and stop |
| `AjaxSpider_GetResults` | Ajax Spider result count retrieval and result structure validation |

---

## Test Infrastructure

### MockHttpMessageHandler

Mock HTTP handler for unit tests. Returns responses based on URL pattern matching.

- `When(urlContains, jsonResponse)` — Match URL substring and return JSON response
- `WhenBytes(urlContains, bytes, contentType)` — Return binary data
- `WhenStatus(urlContains, statusCode)` — Return a specific HTTP status code
- `Requests` — Captures all sent requests for assertion

### ZapIntegrationFixture

Shared fixture for integration tests. Manages the Docker environment lifecycle.

- `InitializeAsync` starts containers and waits for health checks
- `DisposeAsync` only stops containers when `INTEGRATION_TEST_CLEANUP=true` environment variable is set
- Provides a `ZapClient` property for accessing the `ZapApiClient` instance

### Vulnerable Test Web Application

An intentionally vulnerable Flask web application used as a scan target for ZAP integration tests.

| Endpoint | Vulnerability | Description |
|---|---|---|
| `/` | — | Home page with links to vulnerable endpoints |
| `/health` | — | Health check |
| `/search?q=` | Reflected XSS | Query parameter rendered without escaping |
| `/login` | CSRF | Login form without CSRF token |
| `/admin` | Broken auth | Session check only, no additional controls |
| `/users?id=` | SQL Injection | Non-parameterized query with string formatting |
| `/redirect?url=` | Open Redirect | No URL validation for redirect target |
| `/about` | — | Static content |
| `/contact` | — | Static content |
