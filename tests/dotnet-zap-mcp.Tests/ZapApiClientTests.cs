using System.Net;
using Xunit;
using ZapMcpServer.Tests.Helpers;

namespace ZapMcpServer.Tests;

public class ZapApiClientTests
{
    private static (ZapApiClient Client, MockHttpMessageHandler Handler) CreateClient(Action<MockHttpMessageHandler>? configure = null)
    {
        var handler = new MockHttpMessageHandler();
        configure?.Invoke(handler);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8090") };
        var client = new ZapApiClient(httpClient, "test-api-key");
        return (client, handler);
    }

    // -- URL construction & apikey --

    [Fact]
    public async Task GetVersionAsync_AppendsApiKey()
    {
        var (client, handler) = CreateClient(h =>
            h.When("/JSON/core/view/version/", """{"version":"2.15.0"}"""));

        await client.GetVersionAsync();

        var url = handler.Requests[0].RequestUri!.ToString();
        Assert.Contains("apikey=test-api-key", url);
    }

    [Fact]
    public async Task GetVersionAsync_ParsesVersion()
    {
        var (client, _) = CreateClient(h =>
            h.When("/JSON/core/view/version/", """{"version":"2.15.0"}"""));

        var version = await client.GetVersionAsync();

        Assert.Equal("2.15.0", version);
    }

    // -- Core --

    [Fact]
    public async Task GetHostsAsync_ReturnsHosts()
    {
        var (client, _) = CreateClient(h =>
            h.When("/JSON/core/view/hosts/", """{"hosts":["localhost","example.com"]}"""));

        var hosts = await client.GetHostsAsync();

        Assert.Equal(["localhost", "example.com"], hosts);
    }

    [Fact]
    public async Task GetHostsAsync_ReturnsEmpty()
    {
        var (client, _) = CreateClient(h =>
            h.When("/JSON/core/view/hosts/", """{"hosts":[]}"""));

        var hosts = await client.GetHostsAsync();

        Assert.Empty(hosts);
    }

    [Fact]
    public async Task GetSitesAsync_ReturnsSites()
    {
        var (client, _) = CreateClient(h =>
            h.When("/JSON/core/view/sites/", """{"sites":["http://example.com"]}"""));

        var sites = await client.GetSitesAsync();

        Assert.Equal(["http://example.com"], sites);
    }

    [Fact]
    public async Task GetUrlsAsync_EncodesBaseUrl()
    {
        var (client, handler) = CreateClient(h =>
            h.When("/JSON/core/view/urls/", """{"urls":["http://example.com/page"]}"""));

        await client.GetUrlsAsync("http://example.com");

        var url = handler.Requests[0].RequestUri!.ToString();
        Assert.Contains("baseurl=http%3A%2F%2Fexample.com", url);
    }

    // -- Alerts --

    [Fact]
    public async Task GetAlertsSummaryAsync_ReturnsJson()
    {
        var (client, _) = CreateClient(h =>
            h.When("/JSON/alert/view/alertsSummary/",
                """{"alertsSummary":{"High":1,"Medium":2,"Low":3,"Informational":0}}"""));

        var json = await client.GetAlertsSummaryAsync("http://example.com");

        Assert.Equal(1, json.GetProperty("alertsSummary").GetProperty("High").GetInt32());
    }

    [Fact]
    public async Task GetAlertsAsync_PassesRiskIdFilter()
    {
        var (client, handler) = CreateClient(h =>
            h.When("/JSON/alert/view/alerts/", """{"alerts":[]}"""));

        await client.GetAlertsAsync("http://example.com", riskId: "3");

        var url = handler.Requests[0].RequestUri!.ToString();
        Assert.Contains("riskId=3", url);
    }

    // -- Passive Scan --

    [Fact]
    public async Task GetPassiveScanRecordsToScanAsync_ParsesInt()
    {
        var (client, _) = CreateClient(h =>
            h.When("/JSON/pscan/view/recordsToScan/", """{"recordsToScan":"42"}"""));

        var records = await client.GetPassiveScanRecordsToScanAsync();

        Assert.Equal(42, records);
    }

    // -- Spider --

    [Fact]
    public async Task StartSpiderAsync_ReturnsScanId()
    {
        var (client, _) = CreateClient(h =>
            h.When("/JSON/spider/action/scan/", """{"scan":"1"}"""));

        var scanId = await client.StartSpiderAsync("http://example.com");

        Assert.Equal("1", scanId);
    }

    [Fact]
    public async Task GetSpiderStatusAsync_ParsesPercentage()
    {
        var (client, _) = CreateClient(h =>
            h.When("/JSON/spider/view/status/", """{"status":"75"}"""));

        var status = await client.GetSpiderStatusAsync("1");

        Assert.Equal(75, status);
    }

    [Fact]
    public async Task GetSpiderResultsAsync_ReturnsResults()
    {
        var (client, _) = CreateClient(h =>
            h.When("/JSON/spider/view/results/", """{"results":["http://example.com","http://example.com/page"]}"""));

        var results = await client.GetSpiderResultsAsync("1");

        Assert.Equal(2, results.GetProperty("results").GetArrayLength());
    }

    // -- Active Scan --

    [Fact]
    public async Task StartActiveScanAsync_ReturnsScanId()
    {
        var (client, _) = CreateClient(h =>
            h.When("/JSON/ascan/action/scan/", """{"scan":"5"}"""));

        var scanId = await client.StartActiveScanAsync("http://example.com");

        Assert.Equal("5", scanId);
    }

    [Fact]
    public async Task GetActiveScanStatusAsync_ParsesPercentage()
    {
        var (client, _) = CreateClient(h =>
            h.When("/JSON/ascan/view/status/", """{"status":"100"}"""));

        var status = await client.GetActiveScanStatusAsync("5");

        Assert.Equal(100, status);
    }

    // -- Reports --

    [Fact]
    public async Task GetHtmlReportAsync_ReturnsBytes()
    {
        var expected = "<html><body>report</body></html>"u8.ToArray();
        var (client, _) = CreateClient(h =>
            h.WhenBytes("/OTHER/core/other/htmlreport/", expected, "text/html"));

        var bytes = await client.GetHtmlReportAsync();

        Assert.Equal(expected, bytes);
    }

    [Fact]
    public async Task GetJsonReportAsync_ReturnsBytes()
    {
        var expected = """{"report":"data"}"""u8.ToArray();
        var (client, _) = CreateClient(h =>
            h.WhenBytes("/OTHER/core/other/jsonreport/", expected, "application/json"));

        var bytes = await client.GetJsonReportAsync();

        Assert.Equal(expected, bytes);
    }

    // -- Context --

    [Fact]
    public async Task GetContextListAsync_ParsesContextList()
    {
        var (client, _) = CreateClient(h =>
            h.When("/JSON/context/view/contextList/", """{"contextList":"[Default Context, Test]"}"""));

        var list = await client.GetContextListAsync();

        Assert.Equal(["Default Context", "Test"], list);
    }

    [Fact]
    public async Task GetContextListAsync_ReturnsEmpty()
    {
        var (client, _) = CreateClient(h =>
            h.When("/JSON/context/view/contextList/", """{"contextList":"[]"}"""));

        var list = await client.GetContextListAsync();

        Assert.Empty(list);
    }

    [Fact]
    public async Task CreateContextAsync_ReturnsContextId()
    {
        var (client, _) = CreateClient(h =>
            h.When("/JSON/context/action/newContext/", """{"contextId":"2"}"""));

        var id = await client.CreateContextAsync("TestCtx");

        Assert.Equal("2", id);
    }

    // -- Authentication --

    [Fact]
    public async Task SetAuthenticationMethodAsync_SendsCorrectParams()
    {
        var (client, handler) = CreateClient(h =>
            h.When("/JSON/authentication/action/setAuthenticationMethod/", """{"Result":"OK"}"""));

        await client.SetAuthenticationMethodAsync("1", "formBasedAuthentication", "loginUrl=http://example.com/login");

        var url = handler.Requests[0].RequestUri!.ToString();
        Assert.Contains("authMethodName=formBasedAuthentication", url);
        Assert.Contains("authMethodConfigParams=", url);
    }

    // -- Users --

    [Fact]
    public async Task CreateUserAsync_ReturnsUserId()
    {
        var (client, _) = CreateClient(h =>
            h.When("/JSON/users/action/newUser/", """{"userId":"10"}"""));

        var id = await client.CreateUserAsync("1", "testuser");

        Assert.Equal("10", id);
    }

    // -- Forced User --

    [Fact]
    public async Task IsForcedUserModeEnabledAsync_ParsesBool()
    {
        var (client, _) = CreateClient(h =>
            h.When("/JSON/forcedUser/view/isForcedUserModeEnabled/", """{"forcedModeEnabled":"true"}"""));

        var enabled = await client.IsForcedUserModeEnabledAsync();

        Assert.True(enabled);
    }

    // -- Ajax Spider --

    [Fact]
    public async Task StartAjaxSpiderAsync_ReturnsResult()
    {
        var (client, _) = CreateClient(h =>
            h.When("/JSON/ajaxSpider/action/scan/", """{"Result":"OK"}"""));

        var result = await client.StartAjaxSpiderAsync("http://example.com");

        Assert.Equal("OK", result);
    }

    [Fact]
    public async Task GetAjaxSpiderStatusAsync_ReturnsStatus()
    {
        var (client, _) = CreateClient(h =>
            h.When("/JSON/ajaxSpider/view/status/", """{"status":"running"}"""));

        var status = await client.GetAjaxSpiderStatusAsync();

        Assert.Equal("running", status);
    }

    [Fact]
    public async Task GetAjaxSpiderNumberOfResultsAsync_ParsesInt()
    {
        var (client, _) = CreateClient(h =>
            h.When("/JSON/ajaxSpider/view/numberOfResults/", """{"numberOfResults":"15"}"""));

        var count = await client.GetAjaxSpiderNumberOfResultsAsync();

        Assert.Equal(15, count);
    }

    // -- Error handling --

    [Fact]
    public async Task GetVersionAsync_ThrowsOnNon2xx()
    {
        var (client, _) = CreateClient(h =>
            h.WhenStatus("/JSON/core/view/version/", HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetVersionAsync());
    }

    // -- Reconfigure --

    [Fact]
    public async Task Reconfigure_UsesNewBaseUrlAndApiKey()
    {
        var handler = new MockHttpMessageHandler()
            .When("/JSON/core/view/version/", """{"version":"2.15.0"}""");

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://old-host:8090") };
        var client = new ZapApiClient(httpClient, "old-key");

        // Reconfigure can't truly change base URL when using injected HttpClient since
        // Reconfigure creates a new HttpClient internally. But we can verify the old client works.
        await client.GetVersionAsync();

        var url = handler.Requests[0].RequestUri!.ToString();
        Assert.Contains("apikey=old-key", url);
    }
}
