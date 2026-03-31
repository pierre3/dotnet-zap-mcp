using System.Text.Json;
using Xunit;

namespace ZapMcpServer.Tests.IntegrationTests;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class ZapAlertsIntegrationTests
{
    private readonly ZapApiClient _zap;

    public ZapAlertsIntegrationTests(ZapIntegrationFixture fixture)
    {
        _zap = fixture.ZapClient;
    }

    [Fact]
    public async Task GetAlertsSummary_ReturnsValidStructure()
    {
        // Run spider first to generate some traffic
        var spiderScanId = await _zap.StartSpiderAsync(ZapIntegrationFixture.TargetUrl);
        var deadline = DateTime.UtcNow.AddSeconds(120);
        while (await _zap.GetSpiderStatusAsync(spiderScanId) < 100)
        {
            Assert.True(DateTime.UtcNow < deadline, "Spider timed out");
            await Task.Delay(2000);
        }

        // Wait for passive scan
        var pscanDeadline = DateTime.UtcNow.AddSeconds(60);
        while (await _zap.GetPassiveScanRecordsToScanAsync() > 0)
        {
            Assert.True(DateTime.UtcNow < pscanDeadline, "Passive scan timed out");
            await Task.Delay(2000);
        }

        var summary = await _zap.GetAlertsSummaryAsync(ZapIntegrationFixture.TargetUrl);
        var alertsSummary = summary.GetProperty("alertsSummary");

        // Should have risk level keys
        Assert.True(alertsSummary.TryGetProperty("High", out _) ||
                     alertsSummary.TryGetProperty("Medium", out _) ||
                     alertsSummary.TryGetProperty("Low", out _) ||
                     alertsSummary.TryGetProperty("Informational", out _),
            "Summary should contain at least one risk level");
    }

    [Fact]
    public async Task GetAlerts_ReturnsAlertList()
    {
        // Run spider first
        var spiderScanId = await _zap.StartSpiderAsync(ZapIntegrationFixture.TargetUrl);
        var deadline = DateTime.UtcNow.AddSeconds(120);
        while (await _zap.GetSpiderStatusAsync(spiderScanId) < 100)
        {
            Assert.True(DateTime.UtcNow < deadline, "Spider timed out");
            await Task.Delay(2000);
        }

        // Wait for passive scan
        var pscanDeadline = DateTime.UtcNow.AddSeconds(60);
        while (await _zap.GetPassiveScanRecordsToScanAsync() > 0)
        {
            Assert.True(DateTime.UtcNow < pscanDeadline, "Passive scan timed out");
            await Task.Delay(2000);
        }

        var json = await _zap.GetAlertsAsync(ZapIntegrationFixture.TargetUrl, count: 10);
        var alerts = json.GetProperty("alerts");

        Assert.True(alerts.ValueKind == JsonValueKind.Array);

        // The vulnerable app should generate at least some passive scan alerts
        // (e.g., missing security headers)
        if (alerts.GetArrayLength() > 0)
        {
            var first = alerts[0];
            Assert.True(first.TryGetProperty("name", out _));
            Assert.True(first.TryGetProperty("risk", out _));
            Assert.True(first.TryGetProperty("url", out _));
        }
    }

    [Fact]
    public async Task GetAlerts_WithRiskIdFilter()
    {
        var allAlerts = await _zap.GetAlertsAsync(ZapIntegrationFixture.TargetUrl, count: 100);
        var filteredAlerts = await _zap.GetAlertsAsync(ZapIntegrationFixture.TargetUrl, count: 100, riskId: "0");

        // Filtered count should be <= total count
        Assert.True(
            filteredAlerts.GetProperty("alerts").GetArrayLength() <=
            allAlerts.GetProperty("alerts").GetArrayLength());
    }
}
