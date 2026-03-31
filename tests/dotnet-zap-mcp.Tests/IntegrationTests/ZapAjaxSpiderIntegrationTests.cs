using Xunit;

namespace ZapMcpServer.Tests.IntegrationTests;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class ZapAjaxSpiderIntegrationTests
{
    private readonly ZapApiClient _zap;

    public ZapAjaxSpiderIntegrationTests(ZapIntegrationFixture fixture)
    {
        _zap = fixture.ZapClient;
    }

    [Fact]
    public async Task AjaxSpider_StartAndStop()
    {
        // Start Ajax Spider
        var result = await _zap.StartAjaxSpiderAsync(ZapIntegrationFixture.TargetUrl);
        Assert.Equal("OK", result);

        // Check status
        var status = await _zap.GetAjaxSpiderStatusAsync();
        Assert.True(status == "running" || status == "stopped",
            $"Expected 'running' or 'stopped', got '{status}'");

        // Wait briefly for some results
        await Task.Delay(5000);

        // Stop
        await _zap.StopAjaxSpiderAsync();

        // Verify stopped
        var statusAfter = await _zap.GetAjaxSpiderStatusAsync();
        Assert.Equal("stopped", statusAfter);
    }

    [Fact]
    public async Task AjaxSpider_GetResults()
    {
        // Start and let it run briefly
        await _zap.StartAjaxSpiderAsync(ZapIntegrationFixture.TargetUrl);
        await Task.Delay(10000);
        await _zap.StopAjaxSpiderAsync();

        // Get number of results
        var count = await _zap.GetAjaxSpiderNumberOfResultsAsync();
        Assert.True(count >= 0);

        // Get full results if any
        if (count > 0)
        {
            var fullResults = await _zap.GetAjaxSpiderFullResultsAsync();
            Assert.True(fullResults.TryGetProperty("fullResults", out var results));
            // fullResults can be an array or an object depending on ZAP version
            if (results.ValueKind == System.Text.Json.JsonValueKind.Array)
                Assert.True(results.GetArrayLength() > 0);
            else
                Assert.True(results.ValueKind == System.Text.Json.JsonValueKind.Object);
        }
    }
}
