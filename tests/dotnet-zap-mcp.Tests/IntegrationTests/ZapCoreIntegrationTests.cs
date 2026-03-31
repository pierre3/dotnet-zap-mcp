using Xunit;
namespace ZapMcpServer.Tests.IntegrationTests;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class ZapCoreIntegrationTests
{
    private readonly ZapApiClient _zap;

    public ZapCoreIntegrationTests(ZapIntegrationFixture fixture)
    {
        _zap = fixture.ZapClient;
    }

    [Fact]
    public async Task GetVersion_ReturnsVersionString()
    {
        var version = await _zap.GetVersionAsync();

        Assert.False(string.IsNullOrEmpty(version));
    }

    [Fact]
    public async Task GetHosts_ReturnsSuccessfully()
    {
        var hosts = await _zap.GetHostsAsync();

        Assert.NotNull(hosts);
    }

    [Fact]
    public async Task GetSites_ReturnsSuccessfully()
    {
        var sites = await _zap.GetSitesAsync();

        Assert.NotNull(sites);
    }

    [Fact]
    public async Task GetPassiveScanRecordsToScan_ReturnsNonNegative()
    {
        var records = await _zap.GetPassiveScanRecordsToScanAsync();

        Assert.True(records >= 0);
    }

    [Fact]
    public async Task GetHtmlReport_ReturnsNonEmptyBytes()
    {
        var bytes = await _zap.GetHtmlReportAsync();

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task GetJsonReport_ReturnsNonEmptyBytes()
    {
        var bytes = await _zap.GetJsonReportAsync();

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task GetXmlReport_ReturnsNonEmptyBytes()
    {
        var bytes = await _zap.GetXmlReportAsync();

        Assert.NotEmpty(bytes);
    }
}
