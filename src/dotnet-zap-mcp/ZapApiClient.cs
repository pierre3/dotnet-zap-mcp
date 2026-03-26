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

    public async Task<JsonElement> GetAlertsAsync(string baseUrl, int start = 0, int count = 100, CancellationToken ct = default)
    {
        return await GetJsonAsync(
            $"/JSON/alert/view/alerts/?baseurl={Uri.EscapeDataString(baseUrl)}&start={start}&count={count}", ct);
    }

    // -- Passive Scan --

    public async Task<int> GetPassiveScanRecordsToScanAsync(CancellationToken ct = default)
    {
        var json = await GetJsonAsync("/JSON/pscan/view/recordsToScan/", ct);
        return int.Parse(json.GetProperty("recordsToScan").GetString() ?? "0");
    }

    // -- Spider --

    public async Task<string> StartSpiderAsync(string url, int maxChildren = 0, bool recurse = true, CancellationToken ct = default)
    {
        var json = await GetJsonAsync(
            $"/JSON/spider/action/scan/?url={Uri.EscapeDataString(url)}&maxchildren={maxChildren}&recurse={recurse}", ct);
        return json.GetProperty("scan").GetString() ?? "";
    }

    public async Task<int> GetSpiderStatusAsync(string scanId, CancellationToken ct = default)
    {
        var json = await GetJsonAsync($"/JSON/spider/view/status/?scanId={scanId}", ct);
        return int.Parse(json.GetProperty("status").GetString() ?? "0");
    }

    // -- Active Scan --

    public async Task<string> StartActiveScanAsync(string url, bool recurse = true, string scanPolicyName = "", CancellationToken ct = default)
    {
        var policyParam = string.IsNullOrEmpty(scanPolicyName) ? "" : $"&scanPolicyName={Uri.EscapeDataString(scanPolicyName)}";
        var json = await GetJsonAsync(
            $"/JSON/ascan/action/scan/?url={Uri.EscapeDataString(url)}&recurse={recurse}{policyParam}", ct);
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
}
