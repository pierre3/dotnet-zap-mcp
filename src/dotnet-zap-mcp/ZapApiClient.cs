using System.Text.Json;

namespace ZapMcpServer;

/// <summary>
/// ZAP REST API wrapper client.
/// </summary>
internal sealed class ZapApiClient
{
    private HttpClient _http;
    private string _apiKey;
    private readonly object _lock = new();

    public ZapApiClient(string baseUrl, string apiKey)
    {
        ArgumentNullException.ThrowIfNull(baseUrl);
        _apiKey = apiKey ?? "";
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    /// <summary>
    /// Reconfigure the client with a new base URL and API key.
    /// Used by DockerComposeTools after starting the ZAP container.
    /// </summary>
    public void Reconfigure(string baseUrl, string apiKey)
    {
        lock (_lock)
        {
            _http.Dispose();
            _apiKey = apiKey ?? "";
            _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }
    }

    /// <summary>Send a GET request to the ZAP API and return the JSON response.</summary>
    private async Task<JsonElement> GetJsonAsync(string path, CancellationToken ct = default)
    {
        HttpClient http;
        string apiKey;
        lock (_lock)
        {
            http = _http;
            apiKey = _apiKey;
        }

        var separator = path.Contains('?') ? '&' : '?';
        var url = $"{path}{separator}apikey={apiKey}";
        using var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync(ct);
        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return doc.RootElement.Clone();
    }

    /// <summary>Send a GET request to the ZAP API and return the raw response bytes.</summary>
    private async Task<byte[]> GetBytesAsync(string path, CancellationToken ct = default)
    {
        HttpClient http;
        string apiKey;
        lock (_lock)
        {
            http = _http;
            apiKey = _apiKey;
        }

        var separator = path.Contains('?') ? '&' : '?';
        var url = $"{path}{separator}apikey={apiKey}";
        using var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    // -- Core --

    public async Task<string> GetVersionAsync(CancellationToken ct = default)
    {
        var json = await GetJsonAsync("/JSON/core/view/version/", ct);
        return json.GetProperty("version").GetString() ?? "";
    }

    public async Task<string[]> GetHostsAsync(CancellationToken ct = default)
    {
        var json = await GetJsonAsync("/JSON/core/view/hosts/", ct);
        return json.GetProperty("hosts").EnumerateArray()
            .Select(e => e.GetString() ?? "").ToArray();
    }

    public async Task<string[]> GetSitesAsync(CancellationToken ct = default)
    {
        var json = await GetJsonAsync("/JSON/core/view/sites/", ct);
        return json.GetProperty("sites").EnumerateArray()
            .Select(e => e.GetString() ?? "").ToArray();
    }

    public async Task<string[]> GetUrlsAsync(string baseUrl, CancellationToken ct = default)
    {
        var json = await GetJsonAsync($"/JSON/core/view/urls/?baseurl={Uri.EscapeDataString(baseUrl)}", ct);
        return json.GetProperty("urls").EnumerateArray()
            .Select(e => e.GetString() ?? "").ToArray();
    }

    public async Task<JsonElement> GetAlertsSummaryAsync(string baseUrl, CancellationToken ct = default)
    {
        return await GetJsonAsync($"/JSON/alert/view/alertsSummary/?baseurl={Uri.EscapeDataString(baseUrl)}", ct);
    }

    public async Task<JsonElement> GetAlertsAsync(string baseUrl, int start = 0, int count = 100, string riskId = "", CancellationToken ct = default)
    {
        var riskParam = string.IsNullOrEmpty(riskId) ? "" : $"&riskId={Uri.EscapeDataString(riskId)}";
        return await GetJsonAsync(
            $"/JSON/alert/view/alerts/?baseurl={Uri.EscapeDataString(baseUrl)}&start={start}&count={count}{riskParam}", ct);
    }

    public async Task<JsonElement> GetAlertsByRiskAsync(string url = "", bool recurse = false, CancellationToken ct = default)
    {
        var urlParam = string.IsNullOrEmpty(url) ? "" : $"url={Uri.EscapeDataString(url)}&";
        return await GetJsonAsync(
            $"/JSON/alert/view/alertsByRisk/?{urlParam}recurse={recurse}", ct);
    }

    // -- Passive Scan --

    public async Task<int> GetPassiveScanRecordsToScanAsync(CancellationToken ct = default)
    {
        var json = await GetJsonAsync("/JSON/pscan/view/recordsToScan/", ct);
        return int.Parse(json.GetProperty("recordsToScan").GetString() ?? "0");
    }

    // -- Spider --

    public async Task<string> StartSpiderAsync(string url, int maxChildren = 0, bool recurse = true, bool subtreeOnly = false, string contextName = "", CancellationToken ct = default)
    {
        var contextParam = string.IsNullOrEmpty(contextName) ? "" : $"&contextName={Uri.EscapeDataString(contextName)}";
        var json = await GetJsonAsync(
            $"/JSON/spider/action/scan/?url={Uri.EscapeDataString(url)}&maxchildren={maxChildren}&recurse={recurse}&subtreeOnly={subtreeOnly}{contextParam}", ct);
        return json.GetProperty("scan").GetString() ?? "";
    }

    public async Task<int> GetSpiderStatusAsync(string scanId, CancellationToken ct = default)
    {
        var json = await GetJsonAsync($"/JSON/spider/view/status/?scanId={scanId}", ct);
        return int.Parse(json.GetProperty("status").GetString() ?? "0");
    }

    public async Task<JsonElement> GetSpiderResultsAsync(string scanId, CancellationToken ct = default)
    {
        return await GetJsonAsync($"/JSON/spider/view/results/?scanId={scanId}", ct);
    }

    public async Task<JsonElement> GetSpiderFullResultsAsync(string scanId, CancellationToken ct = default)
    {
        return await GetJsonAsync($"/JSON/spider/view/fullResults/?scanId={scanId}", ct);
    }

    public async Task StopSpiderAsync(string scanId, CancellationToken ct = default)
    {
        await GetJsonAsync($"/JSON/spider/action/stop/?scanId={scanId}", ct);
    }

    // -- Active Scan --

    public async Task<string> StartActiveScanAsync(string url, bool recurse = true, bool inScopeOnly = false, string scanPolicyName = "", string contextId = "", CancellationToken ct = default)
    {
        var policyParam = string.IsNullOrEmpty(scanPolicyName) ? "" : $"&scanPolicyName={Uri.EscapeDataString(scanPolicyName)}";
        var contextParam = string.IsNullOrEmpty(contextId) ? "" : $"&contextId={Uri.EscapeDataString(contextId)}";
        var json = await GetJsonAsync(
            $"/JSON/ascan/action/scan/?url={Uri.EscapeDataString(url)}&recurse={recurse}&inScopeOnly={inScopeOnly}{policyParam}{contextParam}", ct);
        return json.GetProperty("scan").GetString() ?? "";
    }

    public async Task<int> GetActiveScanStatusAsync(string scanId, CancellationToken ct = default)
    {
        var json = await GetJsonAsync($"/JSON/ascan/view/status/?scanId={scanId}", ct);
        return int.Parse(json.GetProperty("status").GetString() ?? "0");
    }

    public async Task StopActiveScanAsync(string scanId, CancellationToken ct = default)
    {
        await GetJsonAsync($"/JSON/ascan/action/stop/?scanId={scanId}", ct);
    }

    // -- Reports --

    public async Task<byte[]> GetHtmlReportAsync(CancellationToken ct = default)
    {
        return await GetBytesAsync("/OTHER/core/other/htmlreport/", ct);
    }

    public async Task<byte[]> GetJsonReportAsync(CancellationToken ct = default)
    {
        return await GetBytesAsync("/OTHER/core/other/jsonreport/", ct);
    }

    public async Task<byte[]> GetXmlReportAsync(CancellationToken ct = default)
    {
        return await GetBytesAsync("/OTHER/core/other/xmlreport/", ct);
    }

    // -- Context --

    public async Task<string[]> GetContextListAsync(CancellationToken ct = default)
    {
        var json = await GetJsonAsync("/JSON/context/view/contextList/", ct);
        var raw = json.GetProperty("contextList").GetString() ?? "[]";
        // ZAP returns contextList as a string like "[Default Context, My Context]"
        raw = raw.Trim('[', ']');
        if (string.IsNullOrWhiteSpace(raw)) return [];
        return raw.Split(',').Select(s => s.Trim()).ToArray();
    }

    public async Task<JsonElement> GetContextAsync(string contextName, CancellationToken ct = default)
    {
        return await GetJsonAsync($"/JSON/context/view/context/?contextName={Uri.EscapeDataString(contextName)}", ct);
    }

    public async Task<string> CreateContextAsync(string contextName, CancellationToken ct = default)
    {
        var json = await GetJsonAsync($"/JSON/context/action/newContext/?contextName={Uri.EscapeDataString(contextName)}", ct);
        return json.GetProperty("contextId").GetString() ?? "";
    }

    public async Task<string> RemoveContextAsync(string contextName, CancellationToken ct = default)
    {
        var json = await GetJsonAsync($"/JSON/context/action/removeContext/?contextName={Uri.EscapeDataString(contextName)}", ct);
        return json.GetProperty("Result").GetString() ?? "";
    }

    public async Task<string> IncludeInContextAsync(string contextName, string regex, CancellationToken ct = default)
    {
        var json = await GetJsonAsync(
            $"/JSON/context/action/includeInContext/?contextName={Uri.EscapeDataString(contextName)}&regex={Uri.EscapeDataString(regex)}", ct);
        return json.GetProperty("Result").GetString() ?? "";
    }

    public async Task<string> ExcludeFromContextAsync(string contextName, string regex, CancellationToken ct = default)
    {
        var json = await GetJsonAsync(
            $"/JSON/context/action/excludeFromContext/?contextName={Uri.EscapeDataString(contextName)}&regex={Uri.EscapeDataString(regex)}", ct);
        return json.GetProperty("Result").GetString() ?? "";
    }

    public async Task<string[]> GetIncludeRegexsAsync(string contextName, CancellationToken ct = default)
    {
        var json = await GetJsonAsync($"/JSON/context/view/includeRegexs/?contextName={Uri.EscapeDataString(contextName)}", ct);
        return json.GetProperty("includeRegexs").EnumerateArray()
            .Select(e => e.GetString() ?? "").ToArray();
    }

    public async Task<string[]> GetExcludeRegexsAsync(string contextName, CancellationToken ct = default)
    {
        var json = await GetJsonAsync($"/JSON/context/view/excludeRegexs/?contextName={Uri.EscapeDataString(contextName)}", ct);
        return json.GetProperty("excludeRegexs").EnumerateArray()
            .Select(e => e.GetString() ?? "").ToArray();
    }

    public async Task<string> ImportContextAsync(string contextFilePath, CancellationToken ct = default)
    {
        var json = await GetJsonAsync(
            $"/JSON/context/action/importContext/?contextFile={Uri.EscapeDataString(contextFilePath)}", ct);
        return json.GetProperty("contextId").GetString() ?? "";
    }

    public async Task<string> ExportContextAsync(string contextName, string contextFilePath, CancellationToken ct = default)
    {
        var json = await GetJsonAsync(
            $"/JSON/context/action/exportContext/?contextName={Uri.EscapeDataString(contextName)}&contextFile={Uri.EscapeDataString(contextFilePath)}", ct);
        return json.GetProperty("Result").GetString() ?? "";
    }

    // -- Authentication --

    public async Task<JsonElement> GetAuthenticationMethodAsync(string contextId, CancellationToken ct = default)
    {
        return await GetJsonAsync($"/JSON/authentication/view/getAuthenticationMethod/?contextId={Uri.EscapeDataString(contextId)}", ct);
    }

    public async Task<string> SetAuthenticationMethodAsync(string contextId, string authMethodName, string authMethodConfigParams = "", CancellationToken ct = default)
    {
        var configParam = string.IsNullOrEmpty(authMethodConfigParams) ? "" : $"&authMethodConfigParams={Uri.EscapeDataString(authMethodConfigParams)}";
        var json = await GetJsonAsync(
            $"/JSON/authentication/action/setAuthenticationMethod/?contextId={Uri.EscapeDataString(contextId)}&authMethodName={Uri.EscapeDataString(authMethodName)}{configParam}", ct);
        return json.GetProperty("Result").GetString() ?? "";
    }

    public async Task<string> SetLoggedInIndicatorAsync(string contextId, string loggedInIndicatorRegex, CancellationToken ct = default)
    {
        var json = await GetJsonAsync(
            $"/JSON/authentication/action/setLoggedInIndicator/?contextId={Uri.EscapeDataString(contextId)}&loggedInIndicatorRegex={Uri.EscapeDataString(loggedInIndicatorRegex)}", ct);
        return json.GetProperty("Result").GetString() ?? "";
    }

    public async Task<string> SetLoggedOutIndicatorAsync(string contextId, string loggedOutIndicatorRegex, CancellationToken ct = default)
    {
        var json = await GetJsonAsync(
            $"/JSON/authentication/action/setLoggedOutIndicator/?contextId={Uri.EscapeDataString(contextId)}&loggedOutIndicatorRegex={Uri.EscapeDataString(loggedOutIndicatorRegex)}", ct);
        return json.GetProperty("Result").GetString() ?? "";
    }

    public async Task<string> GetLoggedInIndicatorAsync(string contextId, CancellationToken ct = default)
    {
        var json = await GetJsonAsync($"/JSON/authentication/view/getLoggedInIndicator/?contextId={Uri.EscapeDataString(contextId)}", ct);
        return json.GetProperty("loggedInIndicator").GetString() ?? "";
    }

    public async Task<string> GetLoggedOutIndicatorAsync(string contextId, CancellationToken ct = default)
    {
        var json = await GetJsonAsync($"/JSON/authentication/view/getLoggedOutIndicator/?contextId={Uri.EscapeDataString(contextId)}", ct);
        return json.GetProperty("loggedOutIndicator").GetString() ?? "";
    }

    // -- Users --

    public async Task<JsonElement> GetUsersListAsync(string contextId, CancellationToken ct = default)
    {
        return await GetJsonAsync($"/JSON/users/view/usersList/?contextId={Uri.EscapeDataString(contextId)}", ct);
    }

    public async Task<string> CreateUserAsync(string contextId, string name, CancellationToken ct = default)
    {
        var json = await GetJsonAsync(
            $"/JSON/users/action/newUser/?contextId={Uri.EscapeDataString(contextId)}&name={Uri.EscapeDataString(name)}", ct);
        return json.GetProperty("userId").GetString() ?? "";
    }

    public async Task<string> RemoveUserAsync(string contextId, string userId, CancellationToken ct = default)
    {
        var json = await GetJsonAsync(
            $"/JSON/users/action/removeUser/?contextId={Uri.EscapeDataString(contextId)}&userId={Uri.EscapeDataString(userId)}", ct);
        return json.GetProperty("Result").GetString() ?? "";
    }

    public async Task<string> SetAuthenticationCredentialsAsync(string contextId, string userId, string authCredentialsConfigParams, CancellationToken ct = default)
    {
        var json = await GetJsonAsync(
            $"/JSON/users/action/setAuthenticationCredentials/?contextId={Uri.EscapeDataString(contextId)}&userId={Uri.EscapeDataString(userId)}&authCredentialsConfigParams={Uri.EscapeDataString(authCredentialsConfigParams)}", ct);
        return json.GetProperty("Result").GetString() ?? "";
    }

    public async Task<string> SetUserEnabledAsync(string contextId, string userId, bool enabled, CancellationToken ct = default)
    {
        var json = await GetJsonAsync(
            $"/JSON/users/action/setUserEnabled/?contextId={Uri.EscapeDataString(contextId)}&userId={Uri.EscapeDataString(userId)}&enabled={enabled}", ct);
        return json.GetProperty("Result").GetString() ?? "";
    }

    // -- Forced User --

    public async Task<string> SetForcedUserAsync(string contextId, string userId, CancellationToken ct = default)
    {
        var json = await GetJsonAsync(
            $"/JSON/forcedUser/action/setForcedUser/?contextId={Uri.EscapeDataString(contextId)}&userId={Uri.EscapeDataString(userId)}", ct);
        return json.GetProperty("Result").GetString() ?? "";
    }

    public async Task<string> SetForcedUserModeEnabledAsync(bool enabled, CancellationToken ct = default)
    {
        var json = await GetJsonAsync($"/JSON/forcedUser/action/setForcedUserModeEnabled/?boolean={enabled}", ct);
        return json.GetProperty("Result").GetString() ?? "";
    }

    public async Task<string> GetForcedUserAsync(string contextId, CancellationToken ct = default)
    {
        var json = await GetJsonAsync($"/JSON/forcedUser/view/getForcedUser/?contextId={Uri.EscapeDataString(contextId)}", ct);
        return json.GetProperty("forcedUserId").GetString() ?? "";
    }

    public async Task<bool> IsForcedUserModeEnabledAsync(CancellationToken ct = default)
    {
        var json = await GetJsonAsync("/JSON/forcedUser/view/isForcedUserModeEnabled/", ct);
        return json.GetProperty("forcedModeEnabled").GetString() == "true";
    }

    // -- Ajax Spider --

    public async Task<string> StartAjaxSpiderAsync(string url, bool inScope = false, string contextName = "", bool subtreeOnly = false, CancellationToken ct = default)
    {
        var contextParam = string.IsNullOrEmpty(contextName) ? "" : $"&contextName={Uri.EscapeDataString(contextName)}";
        var json = await GetJsonAsync(
            $"/JSON/ajaxSpider/action/scan/?url={Uri.EscapeDataString(url)}&inScope={inScope}&subtreeOnly={subtreeOnly}{contextParam}", ct);
        return json.GetProperty("Result").GetString() ?? "";
    }

    public async Task<string> StartAjaxSpiderAsUserAsync(string contextName, string userId, string url, bool subtreeOnly = false, CancellationToken ct = default)
    {
        var json = await GetJsonAsync(
            $"/JSON/ajaxSpider/action/scanAsUser/?contextName={Uri.EscapeDataString(contextName)}&userId={Uri.EscapeDataString(userId)}&url={Uri.EscapeDataString(url)}&subtreeOnly={subtreeOnly}", ct);
        return json.GetProperty("Result").GetString() ?? "";
    }

    public async Task<string> GetAjaxSpiderStatusAsync(CancellationToken ct = default)
    {
        var json = await GetJsonAsync("/JSON/ajaxSpider/view/status/", ct);
        return json.GetProperty("status").GetString() ?? "";
    }

    public async Task<int> GetAjaxSpiderNumberOfResultsAsync(CancellationToken ct = default)
    {
        var json = await GetJsonAsync("/JSON/ajaxSpider/view/numberOfResults/", ct);
        return int.Parse(json.GetProperty("numberOfResults").GetString() ?? "0");
    }

    public async Task<JsonElement> GetAjaxSpiderFullResultsAsync(CancellationToken ct = default)
    {
        return await GetJsonAsync("/JSON/ajaxSpider/view/fullResults/", ct);
    }

    public async Task StopAjaxSpiderAsync(CancellationToken ct = default)
    {
        await GetJsonAsync("/JSON/ajaxSpider/action/stop/", ct);
    }
}
