using Xunit;

namespace ZapMcpServer.Tests.IntegrationTests;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class ZapSpiderIntegrationTests
{
    private readonly ZapApiClient _zap;

    public ZapSpiderIntegrationTests(ZapIntegrationFixture fixture)
    {
        _zap = fixture.ZapClient;
    }

    [Fact]
    public async Task Spider_FullWorkflow()
    {
        // Start spider
        var scanId = await _zap.StartSpiderAsync(ZapIntegrationFixture.TargetUrl);
        Assert.False(string.IsNullOrEmpty(scanId));

        // Poll until complete (timeout 120s)
        var deadline = DateTime.UtcNow.AddSeconds(120);
        int status;
        do
        {
            await Task.Delay(2000);
            status = await _zap.GetSpiderStatusAsync(scanId);
            Assert.True(DateTime.UtcNow < deadline, "Spider scan timed out after 120s");
        } while (status < 100);

        Assert.Equal(100, status);

        // Get results
        var results = await _zap.GetSpiderResultsAsync(scanId);
        var urls = results.GetProperty("results");
        Assert.True(urls.GetArrayLength() > 0, "Spider should discover at least one URL");

        // Verify some expected URLs were found
        var urlStrings = urls.EnumerateArray()
            .Select(e => e.GetString() ?? "")
            .ToList();

        Assert.Contains(urlStrings, u => u.Contains("/search"));
        Assert.Contains(urlStrings, u => u.Contains("/login"));
    }

    [Fact]
    public async Task Spider_StopWhileRunning()
    {
        var scanId = await _zap.StartSpiderAsync(ZapIntegrationFixture.TargetUrl);
        Assert.False(string.IsNullOrEmpty(scanId));

        // Brief delay then stop
        await Task.Delay(1000);
        await _zap.StopSpiderAsync(scanId);

        // Status should be retrievable after stop
        var status = await _zap.GetSpiderStatusAsync(scanId);
        Assert.True(status >= 0);
    }
}
