using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace ZapMcpServer;

/// <summary>
/// ZAP MCP tool definitions. All tools callable by agents are defined here.
/// </summary>
[McpServerToolType]
internal sealed class ZapTools
{
    private readonly ZapApiClient _zap;

    public ZapTools(ZapApiClient zap)
    {
        _zap = zap;
    }

    // -- Core --

    [McpServerTool, Description("Get ZAP version to verify connectivity.")]
    public async Task<string> GetVersionAsync(CancellationToken ct)
    {
        var version = await _zap.GetVersionAsync(ct);
        return $"ZAP version: {version}";
    }

    [McpServerTool, Description("List hosts that ZAP has recorded through the proxy.")]
    public async Task<string> GetHostsAsync(CancellationToken ct)
    {
        var hosts = await _zap.GetHostsAsync(ct);
        return hosts.Length == 0
            ? "No hosts recorded yet."
            : string.Join("\n", hosts);
    }

    [McpServerTool, Description("List sites that ZAP has recorded through the proxy.")]
    public async Task<string> GetSitesAsync(CancellationToken ct)
    {
        var sites = await _zap.GetSitesAsync(ct);
        return sites.Length == 0
            ? "No sites recorded yet."
            : string.Join("\n", sites);
    }

    [McpServerTool, Description("List recorded URLs for a given base URL (e.g. http://host.docker.internal:59803).")]
    public async Task<string> GetUrlsAsync(
        [Description("Base URL to filter (e.g. http://host.docker.internal:59803)")] string baseUrl,
        CancellationToken ct)
    {
        var urls = await _zap.GetUrlsAsync(baseUrl, ct);
        return urls.Length == 0
            ? "No URLs recorded for this base URL."
            : $"Recorded URLs ({urls.Length}):\n{string.Join("\n", urls)}";
    }

    // -- Passive Scan --

    [McpServerTool, Description("Get number of records remaining for passive scan.")]
    public async Task<string> GetPassiveScanStatusAsync(CancellationToken ct)
    {
        var remaining = await _zap.GetPassiveScanRecordsToScanAsync(ct);
        return remaining == 0
            ? "Passive scan complete (0 records remaining)."
            : $"Passive scan in progress: {remaining} records remaining.";
    }

    // -- Alerts --

    [McpServerTool, Description("Get alert summary (count by risk level) for a base URL.")]
    public async Task<string> GetAlertsSummaryAsync(
        [Description("Base URL to get alerts for")] string baseUrl,
        CancellationToken ct)
    {
        var json = await _zap.GetAlertsSummaryAsync(baseUrl, ct);
        var summary = json.GetProperty("alertsSummary");
        var sb = new StringBuilder("Alert Summary:\n");
        foreach (var prop in summary.EnumerateObject())
        {
            sb.AppendLine($"  {prop.Name}: {prop.Value}");
        }
        return sb.ToString();
    }

    [McpServerTool, Description("Get detailed alerts for a base URL. Returns alert name, risk, URL, description, and solution.")]
    public async Task<string> GetAlertsAsync(
        [Description("Base URL to get alerts for")] string baseUrl,
        [Description("Start index (default 0)")] int start = 0,
        [Description("Number of alerts to return (default 100)")] int count = 100,
        [Description("Filter by risk level: 0=Informational, 1=Low, 2=Medium, 3=High (default: all)")] string riskId = "",
        CancellationToken ct = default)
    {
        var json = await _zap.GetAlertsAsync(baseUrl, start, count, riskId, ct);
        var alerts = json.GetProperty("alerts");
        var sb = new StringBuilder();
        var index = 0;
        foreach (var alert in alerts.EnumerateArray())
        {
            sb.AppendLine($"--- Alert #{start + index} ---");
            sb.AppendLine($"Name: {alert.GetProperty("name").GetString()}");
            sb.AppendLine($"Risk: {alert.GetProperty("risk").GetString()}");
            sb.AppendLine($"Confidence: {alert.GetProperty("confidence").GetString()}");
            sb.AppendLine($"URL: {alert.GetProperty("url").GetString()}");
            sb.AppendLine($"Parameter: {alert.GetProperty("param").GetString()}");
            sb.AppendLine($"Description: {alert.GetProperty("description").GetString()}");
            sb.AppendLine($"Solution: {alert.GetProperty("solution").GetString()}");
            if (alert.TryGetProperty("cweid", out var cwe))
                sb.AppendLine($"CWE ID: {cwe.GetString()}");
            sb.AppendLine();
            index++;
        }
        return sb.Length == 0 ? "No alerts found." : sb.ToString();
    }

    // -- Spider --

    [McpServerTool, Description("Start a spider scan to crawl a URL and discover pages.")]
    public async Task<string> StartSpiderAsync(
        [Description("Target URL to spider")] string url,
        [Description("Max child nodes per node to crawl (0 = unlimited, default 0)")] int maxChildren = 0,
        [Description("Crawl recursively (default true)")] bool recurse = true,
        [Description("Restrict crawl to subtree of the URL (default false)")] bool subtreeOnly = false,
        [Description("Context name to constrain the spider (optional)")] string contextName = "",
        CancellationToken ct = default)
    {
        var scanId = await _zap.StartSpiderAsync(url, maxChildren, recurse, subtreeOnly, contextName, ct);
        return $"Spider scan started. Scan ID: {scanId}";
    }

    [McpServerTool, Description("Get spider scan progress (0-100%).")]
    public async Task<string> GetSpiderStatusAsync(
        [Description("Spider scan ID")] string scanId,
        CancellationToken ct)
    {
        var status = await _zap.GetSpiderStatusAsync(scanId, ct);
        return status >= 100
            ? "Spider scan complete (100%)."
            : $"Spider scan progress: {status}%";
    }

    [McpServerTool, Description("Get URLs discovered by a spider scan.")]
    public async Task<string> GetSpiderResultsAsync(
        [Description("Spider scan ID")] string scanId,
        CancellationToken ct)
    {
        var json = await _zap.GetSpiderResultsAsync(scanId, ct);
        var results = json.GetProperty("results");
        var urls = results.EnumerateArray().Select(e => e.GetString() ?? "").ToArray();
        return urls.Length == 0
            ? "No URLs discovered yet."
            : $"Discovered URLs ({urls.Length}):\n{string.Join("\n", urls)}";
    }

    [McpServerTool, Description("Stop a running spider scan.")]
    public async Task<string> StopSpiderAsync(
        [Description("Spider scan ID to stop")] string scanId,
        CancellationToken ct)
    {
        await _zap.StopSpiderAsync(scanId, ct);
        return $"Spider scan {scanId} stopped.";
    }

    // -- Active Scan --

    [McpServerTool, Description("Start an active scan (attack test) on a URL. This sends actual attack requests.")]
    public async Task<string> StartActiveScanAsync(
        [Description("Target URL to actively scan")] string url,
        [Description("Scan URLs under the given URL recursively (default true)")] bool recurse = true,
        [Description("Only scan URLs in scope (default false)")] bool inScopeOnly = false,
        [Description("Scan policy name to use (optional)")] string scanPolicyName = "",
        [Description("Context ID to constrain the scan (optional)")] string contextId = "",
        CancellationToken ct = default)
    {
        var scanId = await _zap.StartActiveScanAsync(url, recurse, inScopeOnly, scanPolicyName, contextId, ct);
        return $"Active scan started. Scan ID: {scanId}";
    }

    [McpServerTool, Description("Get active scan progress (0-100%).")]
    public async Task<string> GetActiveScanStatusAsync(
        [Description("Active scan ID")] string scanId,
        CancellationToken ct)
    {
        var status = await _zap.GetActiveScanStatusAsync(scanId, ct);
        return status >= 100
            ? "Active scan complete (100%)."
            : $"Active scan progress: {status}%";
    }

    [McpServerTool, Description("Stop a running active scan.")]
    public async Task<string> StopActiveScanAsync(
        [Description("Active scan ID to stop")] string scanId,
        CancellationToken ct)
    {
        await _zap.StopActiveScanAsync(scanId, ct);
        return $"Active scan {scanId} stopped.";
    }

    // -- Reports --

    [McpServerTool, Description("Generate an HTML scan report containing all alerts found by ZAP.")]
    public async Task<string> GetHtmlReportAsync(CancellationToken ct)
    {
        var bytes = await _zap.GetHtmlReportAsync(ct);
        return Encoding.UTF8.GetString(bytes);
    }

    [McpServerTool, Description("Generate a JSON scan report containing all alerts found by ZAP.")]
    public async Task<string> GetJsonReportAsync(CancellationToken ct)
    {
        var bytes = await _zap.GetJsonReportAsync(ct);
        return Encoding.UTF8.GetString(bytes);
    }

    [McpServerTool, Description("Generate an XML scan report containing all alerts found by ZAP.")]
    public async Task<string> GetXmlReportAsync(CancellationToken ct)
    {
        var bytes = await _zap.GetXmlReportAsync(ct);
        return Encoding.UTF8.GetString(bytes);
    }

    // -- Context --

    [McpServerTool, Description("List all contexts defined in ZAP.")]
    public async Task<string> GetContextListAsync(CancellationToken ct)
    {
        var contexts = await _zap.GetContextListAsync(ct);
        return contexts.Length == 0
            ? "No contexts defined."
            : $"Contexts ({contexts.Length}):\n{string.Join("\n", contexts)}";
    }

    [McpServerTool, Description("Get details of a context (included/excluded URL patterns, technologies, etc.).")]
    public async Task<string> GetContextAsync(
        [Description("Context name")] string contextName,
        CancellationToken ct)
    {
        var json = await _zap.GetContextAsync(contextName, ct);
        var ctx = json.GetProperty("context");
        var sb = new StringBuilder($"Context: {contextName}\n");
        sb.AppendLine($"  ID: {ctx.GetProperty("id").GetString()}");
        sb.AppendLine($"  Description: {ctx.GetProperty("description").GetString()}");
        if (ctx.TryGetProperty("includeRegexs", out var inc))
            sb.AppendLine($"  Include patterns: {inc}");
        if (ctx.TryGetProperty("excludeRegexs", out var exc))
            sb.AppendLine($"  Exclude patterns: {exc}");
        if (ctx.TryGetProperty("inScope", out var scope))
            sb.AppendLine($"  In scope: {scope.GetString()}");
        return sb.ToString();
    }

    [McpServerTool, Description("Create a new context in ZAP. Returns the context ID.")]
    public async Task<string> CreateContextAsync(
        [Description("Name for the new context")] string contextName,
        CancellationToken ct)
    {
        var contextId = await _zap.CreateContextAsync(contextName, ct);
        return $"Context '{contextName}' created. Context ID: {contextId}";
    }

    [McpServerTool, Description("Delete a context from ZAP.")]
    public async Task<string> RemoveContextAsync(
        [Description("Name of the context to remove")] string contextName,
        CancellationToken ct)
    {
        await _zap.RemoveContextAsync(contextName, ct);
        return $"Context '{contextName}' removed.";
    }

    [McpServerTool, Description("Add a URL regex pattern to include in a context's scope (e.g. 'http://example\\.com.*').")]
    public async Task<string> IncludeInContextAsync(
        [Description("Context name")] string contextName,
        [Description("Regex pattern for URLs to include (e.g. http://example\\.com.*)")] string regex,
        CancellationToken ct)
    {
        await _zap.IncludeInContextAsync(contextName, regex, ct);
        return $"Pattern '{regex}' added to context '{contextName}' include list.";
    }

    [McpServerTool, Description("Add a URL regex pattern to exclude from a context's scope (e.g. logout page).")]
    public async Task<string> ExcludeFromContextAsync(
        [Description("Context name")] string contextName,
        [Description("Regex pattern for URLs to exclude (e.g. http://example\\.com/logout)")] string regex,
        CancellationToken ct)
    {
        await _zap.ExcludeFromContextAsync(contextName, regex, ct);
        return $"Pattern '{regex}' added to context '{contextName}' exclude list.";
    }

    [McpServerTool, Description("Import a previously exported context file into ZAP. The file must be accessible inside the ZAP container.")]
    public async Task<string> ImportContextAsync(
        [Description("Path to context file inside the container (e.g. /zap/wrk/data/contexts/mycontext.context)")] string contextFilePath,
        CancellationToken ct)
    {
        var contextId = await _zap.ImportContextAsync(contextFilePath, ct);
        return $"Context imported. Context ID: {contextId}";
    }

    [McpServerTool, Description("Export a context to a file inside the ZAP container for later re-import.")]
    public async Task<string> ExportContextAsync(
        [Description("Context name to export")] string contextName,
        [Description("Destination file path inside the container (e.g. /zap/wrk/data/contexts/mycontext.context)")] string contextFilePath,
        CancellationToken ct)
    {
        await _zap.ExportContextAsync(contextName, contextFilePath, ct);
        return $"Context '{contextName}' exported to {contextFilePath}.";
    }

    // -- Authentication --

    [McpServerTool, Description("Get the authentication method configured for a context.")]
    public async Task<string> GetAuthenticationMethodAsync(
        [Description("Context ID")] string contextId,
        CancellationToken ct)
    {
        var json = await _zap.GetAuthenticationMethodAsync(contextId, ct);
        return json.ToString();
    }

    [McpServerTool, Description("Set the authentication method for a context (e.g. formBasedAuthentication, jsonBasedAuthentication, scriptBasedAuthentication).")]
    public async Task<string> SetAuthenticationMethodAsync(
        [Description("Context ID")] string contextId,
        [Description("Auth method name: formBasedAuthentication, jsonBasedAuthentication, httpAuthentication, or scriptBasedAuthentication")] string authMethodName,
        [Description("URL-encoded config params (e.g. loginUrl=http://example.com/login&loginRequestData=username%3D%7B%25username%25%7D%26password%3D%7B%25password%25%7D)")] string authMethodConfigParams = "",
        CancellationToken ct = default)
    {
        await _zap.SetAuthenticationMethodAsync(contextId, authMethodName, authMethodConfigParams, ct);
        return $"Authentication method '{authMethodName}' set for context {contextId}.";
    }

    [McpServerTool, Description("Set the regex pattern that indicates a user is logged in (matched against responses).")]
    public async Task<string> SetLoggedInIndicatorAsync(
        [Description("Context ID")] string contextId,
        [Description("Regex pattern matching logged-in responses (e.g. \\\\Q<a href=\"logout\">Logout</a>\\\\E)")] string loggedInIndicatorRegex,
        CancellationToken ct)
    {
        await _zap.SetLoggedInIndicatorAsync(contextId, loggedInIndicatorRegex, ct);
        return $"Logged-in indicator set for context {contextId}.";
    }

    [McpServerTool, Description("Set the regex pattern that indicates a user is logged out (matched against responses).")]
    public async Task<string> SetLoggedOutIndicatorAsync(
        [Description("Context ID")] string contextId,
        [Description("Regex pattern matching logged-out responses")] string loggedOutIndicatorRegex,
        CancellationToken ct)
    {
        await _zap.SetLoggedOutIndicatorAsync(contextId, loggedOutIndicatorRegex, ct);
        return $"Logged-out indicator set for context {contextId}.";
    }

    // -- Users --

    [McpServerTool, Description("List all users configured for a context.")]
    public async Task<string> GetUsersListAsync(
        [Description("Context ID")] string contextId,
        CancellationToken ct)
    {
        var json = await _zap.GetUsersListAsync(contextId, ct);
        var users = json.GetProperty("usersList");
        if (users.GetArrayLength() == 0)
            return "No users configured for this context.";

        var sb = new StringBuilder($"Users for context {contextId}:\n");
        foreach (var user in users.EnumerateArray())
        {
            sb.AppendLine($"  ID: {user.GetProperty("id").GetString()}, Name: {user.GetProperty("name").GetString()}, Enabled: {user.GetProperty("enabled").GetString()}");
        }
        return sb.ToString();
    }

    [McpServerTool, Description("Create a new user for a context. Returns the user ID.")]
    public async Task<string> CreateUserAsync(
        [Description("Context ID")] string contextId,
        [Description("Display name for the user")] string name,
        CancellationToken ct)
    {
        var userId = await _zap.CreateUserAsync(contextId, name, ct);
        return $"User '{name}' created. User ID: {userId}";
    }

    [McpServerTool, Description("Remove a user from a context.")]
    public async Task<string> RemoveUserAsync(
        [Description("Context ID")] string contextId,
        [Description("User ID to remove")] string userId,
        CancellationToken ct)
    {
        await _zap.RemoveUserAsync(contextId, userId, ct);
        return $"User {userId} removed from context {contextId}.";
    }

    [McpServerTool, Description("Set authentication credentials for a user (e.g. username and password).")]
    public async Task<string> SetAuthenticationCredentialsAsync(
        [Description("Context ID")] string contextId,
        [Description("User ID")] string userId,
        [Description("URL-encoded credentials (e.g. username=admin&password=secret)")] string authCredentialsConfigParams,
        CancellationToken ct)
    {
        await _zap.SetAuthenticationCredentialsAsync(contextId, userId, authCredentialsConfigParams, ct);
        return $"Credentials set for user {userId} in context {contextId}.";
    }

    [McpServerTool, Description("Enable or disable a user for authenticated scanning.")]
    public async Task<string> SetUserEnabledAsync(
        [Description("Context ID")] string contextId,
        [Description("User ID")] string userId,
        [Description("true to enable, false to disable")] bool enabled,
        CancellationToken ct)
    {
        await _zap.SetUserEnabledAsync(contextId, userId, enabled, ct);
        return $"User {userId} {(enabled ? "enabled" : "disabled")} in context {contextId}.";
    }

    // -- Forced User --

    [McpServerTool, Description("Set the forced user for a context. ZAP will re-authenticate as this user when it detects a logged-out state.")]
    public async Task<string> SetForcedUserAsync(
        [Description("Context ID")] string contextId,
        [Description("User ID to force")] string userId,
        CancellationToken ct)
    {
        await _zap.SetForcedUserAsync(contextId, userId, ct);
        return $"Forced user set to {userId} for context {contextId}.";
    }

    [McpServerTool, Description("Enable or disable forced user mode globally.")]
    public async Task<string> SetForcedUserModeEnabledAsync(
        [Description("true to enable forced user mode, false to disable")] bool enabled,
        CancellationToken ct)
    {
        await _zap.SetForcedUserModeEnabledAsync(enabled, ct);
        return $"Forced user mode {(enabled ? "enabled" : "disabled")}.";
    }

    [McpServerTool, Description("Get the current forced user mode status and forced user for a context.")]
    public async Task<string> GetForcedUserStatusAsync(
        [Description("Context ID")] string contextId,
        CancellationToken ct)
    {
        var modeEnabled = await _zap.IsForcedUserModeEnabledAsync(ct);
        var forcedUserId = await _zap.GetForcedUserAsync(contextId, ct);
        var sb = new StringBuilder("Forced User Status:\n");
        sb.AppendLine($"  Mode enabled: {modeEnabled}");
        sb.AppendLine($"  Forced user ID for context {contextId}: {(string.IsNullOrEmpty(forcedUserId) ? "(none)" : forcedUserId)}");
        return sb.ToString();
    }

    // -- Ajax Spider --

    [McpServerTool, Description("Start the Ajax Spider to crawl a JavaScript-heavy application using a real browser.")]
    public async Task<string> StartAjaxSpiderAsync(
        [Description("Target URL to crawl")] string url,
        [Description("Only crawl URLs in scope (default false)")] bool inScope = false,
        [Description("Context name to constrain the crawl (optional)")] string contextName = "",
        [Description("Restrict crawl to subtree of the URL (default false)")] bool subtreeOnly = false,
        CancellationToken ct = default)
    {
        var result = await _zap.StartAjaxSpiderAsync(url, inScope, contextName, subtreeOnly, ct);
        return $"Ajax Spider started. Result: {result}";
    }

    [McpServerTool, Description("Start the Ajax Spider as a specific user for authenticated crawling.")]
    public async Task<string> StartAjaxSpiderAsUserAsync(
        [Description("Context name")] string contextName,
        [Description("User ID to crawl as")] string userId,
        [Description("Target URL to crawl")] string url,
        [Description("Restrict crawl to subtree of the URL (default false)")] bool subtreeOnly = false,
        CancellationToken ct = default)
    {
        var result = await _zap.StartAjaxSpiderAsUserAsync(contextName, userId, url, subtreeOnly, ct);
        return $"Ajax Spider started as user {userId}. Result: {result}";
    }

    [McpServerTool, Description("Get the current status of the Ajax Spider (running or stopped).")]
    public async Task<string> GetAjaxSpiderStatusAsync(CancellationToken ct)
    {
        var status = await _zap.GetAjaxSpiderStatusAsync(ct);
        return status == "running"
            ? "Ajax Spider is running."
            : $"Ajax Spider status: {status}";
    }

    [McpServerTool, Description("Get the number of results found by the Ajax Spider and a summary.")]
    public async Task<string> GetAjaxSpiderResultsAsync(CancellationToken ct)
    {
        var count = await _zap.GetAjaxSpiderNumberOfResultsAsync(ct);
        if (count == 0)
            return "Ajax Spider has found 0 results.";

        var json = await _zap.GetAjaxSpiderFullResultsAsync(ct);
        var sb = new StringBuilder($"Ajax Spider results ({count} found):\n");
        if (json.TryGetProperty("fullResults", out var results))
        {
            foreach (var item in results.EnumerateArray())
            {
                if (item.TryGetProperty("requestHeader", out var header))
                {
                    var firstLine = header.GetString()?.Split('\n').FirstOrDefault() ?? "";
                    sb.AppendLine($"  {firstLine}");
                }
            }
        }
        return sb.ToString();
    }

    [McpServerTool, Description("Stop the Ajax Spider.")]
    public async Task<string> StopAjaxSpiderAsync(CancellationToken ct)
    {
        await _zap.StopAjaxSpiderAsync(ct);
        return "Ajax Spider stopped.";
    }
}
