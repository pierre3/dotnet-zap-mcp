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
        CancellationToken ct = default)
    {
        var json = await _zap.GetAlertsAsync(baseUrl, start, count, ct);
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
        CancellationToken ct)
    {
        var scanId = await _zap.StartSpiderAsync(url, ct: ct);
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

    // -- Active Scan --

    [McpServerTool, Description("Start an active scan (attack test) on a URL. This sends actual attack requests.")]
    public async Task<string> StartActiveScanAsync(
        [Description("Target URL to actively scan")] string url,
        CancellationToken ct)
    {
        var scanId = await _zap.StartActiveScanAsync(url, ct: ct);
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
}
