using Xunit;

namespace ZapMcpServer.Tests.IntegrationTests;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class ZapActiveScanIntegrationTests
{
    private readonly ZapApiClient _zap;

    public ZapActiveScanIntegrationTests(ZapIntegrationFixture fixture)
    {
        _zap = fixture.ZapClient;
    }

    [Fact]
    public async Task ActiveScan_StartAndMonitor()
    {
        // Ensure spider has run first so there are URLs to scan
        var spiderScanId = await _zap.StartSpiderAsync(ZapIntegrationFixture.TargetUrl);
        var spiderDeadline = DateTime.UtcNow.AddSeconds(120);
        while (await _zap.GetSpiderStatusAsync(spiderScanId) < 100)
        {
            Assert.True(DateTime.UtcNow < spiderDeadline, "Spider timed out");
            await Task.Delay(2000);
        }

        // Wait for passive scan to finish
        var pscanDeadline = DateTime.UtcNow.AddSeconds(60);
        while (await _zap.GetPassiveScanRecordsToScanAsync() > 0)
        {
            Assert.True(DateTime.UtcNow < pscanDeadline, "Passive scan timed out");
            await Task.Delay(2000);
        }

        // Start active scan
        var scanId = await _zap.StartActiveScanAsync(ZapIntegrationFixture.TargetUrl);
        Assert.False(string.IsNullOrEmpty(scanId));

        // Poll status (5 minute timeout for active scan)
        var deadline = DateTime.UtcNow.AddMinutes(5);
        int status;
        do
        {
            await Task.Delay(5000);
            status = await _zap.GetActiveScanStatusAsync(scanId);
            Assert.True(DateTime.UtcNow < deadline, "Active scan timed out after 5 minutes");
        } while (status < 100);

        Assert.Equal(100, status);
    }

    [Fact]
    public async Task ActiveScan_StopWhileRunning()
    {
        // Start spider first
        var spiderScanId = await _zap.StartSpiderAsync(ZapIntegrationFixture.TargetUrl);
        var spiderDeadline = DateTime.UtcNow.AddSeconds(120);
        while (await _zap.GetSpiderStatusAsync(spiderScanId) < 100)
        {
            Assert.True(DateTime.UtcNow < spiderDeadline, "Spider timed out");
            await Task.Delay(2000);
        }

        // Start and quickly stop active scan
        var scanId = await _zap.StartActiveScanAsync(ZapIntegrationFixture.TargetUrl);
        Assert.False(string.IsNullOrEmpty(scanId));

        await Task.Delay(2000);
        await _zap.StopActiveScanAsync(scanId);

        // Status should be retrievable after stop
        var status = await _zap.GetActiveScanStatusAsync(scanId);
        Assert.True(status >= 0);
    }
}
