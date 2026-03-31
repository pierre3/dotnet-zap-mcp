using Xunit;
using ZapMcpServer.Tests.Helpers;

namespace ZapMcpServer.Tests;

public class ZapToolsTests
{
    private static ZapTools CreateTools(Action<MockHttpMessageHandler> configure)
    {
        var handler = new MockHttpMessageHandler();
        configure(handler);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8090") };
        var client = new ZapApiClient(httpClient, "test-key");
        return new ZapTools(client);
    }

    // -- Core --

    [Fact]
    public async Task GetVersionAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/core/view/version/", """{"version":"2.15.0"}"""));

        var result = await tools.GetVersionAsync(CancellationToken.None);

        Assert.Equal("ZAP version: 2.15.0", result);
    }

    [Fact]
    public async Task GetHostsAsync_WithHosts()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/core/view/hosts/", """{"hosts":["localhost","example.com"]}"""));

        var result = await tools.GetHostsAsync(CancellationToken.None);

        Assert.Contains("localhost", result);
        Assert.Contains("example.com", result);
    }

    [Fact]
    public async Task GetHostsAsync_Empty()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/core/view/hosts/", """{"hosts":[]}"""));

        var result = await tools.GetHostsAsync(CancellationToken.None);

        Assert.Equal("No hosts recorded yet.", result);
    }

    [Fact]
    public async Task GetSitesAsync_WithSites()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/core/view/sites/", """{"sites":["http://example.com"]}"""));

        var result = await tools.GetSitesAsync(CancellationToken.None);

        Assert.Contains("http://example.com", result);
    }

    [Fact]
    public async Task GetSitesAsync_Empty()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/core/view/sites/", """{"sites":[]}"""));

        var result = await tools.GetSitesAsync(CancellationToken.None);

        Assert.Equal("No sites recorded yet.", result);
    }

    [Fact]
    public async Task GetUrlsAsync_WithUrls()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/core/view/urls/", """{"urls":["http://example.com/a","http://example.com/b"]}"""));

        var result = await tools.GetUrlsAsync("http://example.com", CancellationToken.None);

        Assert.StartsWith("Recorded URLs (2):", result);
        Assert.Contains("http://example.com/a", result);
    }

    [Fact]
    public async Task GetUrlsAsync_Empty()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/core/view/urls/", """{"urls":[]}"""));

        var result = await tools.GetUrlsAsync("http://example.com", CancellationToken.None);

        Assert.Equal("No URLs recorded for this base URL.", result);
    }

    // -- Passive Scan --

    [Fact]
    public async Task GetPassiveScanStatusAsync_Complete()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/pscan/view/recordsToScan/", """{"recordsToScan":"0"}"""));

        var result = await tools.GetPassiveScanStatusAsync(CancellationToken.None);

        Assert.Equal("Passive scan complete (0 records remaining).", result);
    }

    [Fact]
    public async Task GetPassiveScanStatusAsync_InProgress()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/pscan/view/recordsToScan/", """{"recordsToScan":"42"}"""));

        var result = await tools.GetPassiveScanStatusAsync(CancellationToken.None);

        Assert.Contains("42 records remaining", result);
    }

    // -- Alerts --

    [Fact]
    public async Task GetAlertsSummaryAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/alert/view/alertsSummary/",
                """{"alertsSummary":{"High":1,"Medium":2,"Low":3,"Informational":0}}"""));

        var result = await tools.GetAlertsSummaryAsync("http://example.com", CancellationToken.None);

        Assert.StartsWith("Alert Summary:", result);
        Assert.Contains("High: 1", result);
        Assert.Contains("Medium: 2", result);
    }

    [Fact]
    public async Task GetAlertsAsync_FormatsAlerts()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/alert/view/alerts/", """{"alerts":[{"name":"XSS","risk":"High","confidence":"Medium","url":"http://example.com/search","param":"q","description":"Cross-site scripting","solution":"Encode output","cweid":"79"}]}"""));

        var result = await tools.GetAlertsAsync("http://example.com");

        Assert.Contains("--- Alert #0 ---", result);
        Assert.Contains("Name: XSS", result);
        Assert.Contains("Risk: High", result);
        Assert.Contains("CWE ID: 79", result);
    }

    [Fact]
    public async Task GetAlertsAsync_NoAlerts()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/alert/view/alerts/", """{"alerts":[]}"""));

        var result = await tools.GetAlertsAsync("http://example.com");

        Assert.Equal("No alerts found.", result);
    }

    // -- Spider --

    [Fact]
    public async Task StartSpiderAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/spider/action/scan/", """{"scan":"7"}"""));

        var result = await tools.StartSpiderAsync("http://example.com");

        Assert.Equal("Spider scan started. Scan ID: 7", result);
    }

    [Fact]
    public async Task GetSpiderStatusAsync_Complete()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/spider/view/status/", """{"status":"100"}"""));

        var result = await tools.GetSpiderStatusAsync("7", CancellationToken.None);

        Assert.Equal("Spider scan complete (100%).", result);
    }

    [Fact]
    public async Task GetSpiderStatusAsync_InProgress()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/spider/view/status/", """{"status":"50"}"""));

        var result = await tools.GetSpiderStatusAsync("7", CancellationToken.None);

        Assert.Equal("Spider scan progress: 50%", result);
    }

    [Fact]
    public async Task GetSpiderResultsAsync_WithResults()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/spider/view/results/", """{"results":["http://example.com","http://example.com/page"]}"""));

        var result = await tools.GetSpiderResultsAsync("7", CancellationToken.None);

        Assert.StartsWith("Discovered URLs (2):", result);
    }

    [Fact]
    public async Task GetSpiderResultsAsync_Empty()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/spider/view/results/", """{"results":[]}"""));

        var result = await tools.GetSpiderResultsAsync("7", CancellationToken.None);

        Assert.Equal("No URLs discovered yet.", result);
    }

    [Fact]
    public async Task StopSpiderAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/spider/action/stop/", """{"Result":"OK"}"""));

        var result = await tools.StopSpiderAsync("7", CancellationToken.None);

        Assert.Equal("Spider scan 7 stopped.", result);
    }

    // -- Active Scan --

    [Fact]
    public async Task StartActiveScanAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/ascan/action/scan/", """{"scan":"3"}"""));

        var result = await tools.StartActiveScanAsync("http://example.com");

        Assert.Equal("Active scan started. Scan ID: 3", result);
    }

    [Fact]
    public async Task GetActiveScanStatusAsync_Complete()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/ascan/view/status/", """{"status":"100"}"""));

        var result = await tools.GetActiveScanStatusAsync("3", CancellationToken.None);

        Assert.Equal("Active scan complete (100%).", result);
    }

    [Fact]
    public async Task GetActiveScanStatusAsync_InProgress()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/ascan/view/status/", """{"status":"25"}"""));

        var result = await tools.GetActiveScanStatusAsync("3", CancellationToken.None);

        Assert.Equal("Active scan progress: 25%", result);
    }

    [Fact]
    public async Task StopActiveScanAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/ascan/action/stop/", """{"Result":"OK"}"""));

        var result = await tools.StopActiveScanAsync("3", CancellationToken.None);

        Assert.Equal("Active scan 3 stopped.", result);
    }

    // -- Reports --

    [Fact]
    public async Task GetHtmlReportAsync_ReturnsString()
    {
        var html = "<html><body>report</body></html>";
        var tools = CreateTools(h =>
            h.WhenBytes("/OTHER/core/other/htmlreport/", System.Text.Encoding.UTF8.GetBytes(html), "text/html"));

        var result = await tools.GetHtmlReportAsync(CancellationToken.None);

        Assert.Equal(html, result);
    }

    [Fact]
    public async Task GetJsonReportAsync_ReturnsString()
    {
        var json = """{"report":"data"}""";
        var tools = CreateTools(h =>
            h.WhenBytes("/OTHER/core/other/jsonreport/", System.Text.Encoding.UTF8.GetBytes(json), "application/json"));

        var result = await tools.GetJsonReportAsync(CancellationToken.None);

        Assert.Equal(json, result);
    }

    [Fact]
    public async Task GetXmlReportAsync_ReturnsString()
    {
        var xml = "<report><data/></report>";
        var tools = CreateTools(h =>
            h.WhenBytes("/OTHER/core/other/xmlreport/", System.Text.Encoding.UTF8.GetBytes(xml), "text/xml"));

        var result = await tools.GetXmlReportAsync(CancellationToken.None);

        Assert.Equal(xml, result);
    }

    // -- Context --

    [Fact]
    public async Task GetContextListAsync_WithContexts()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/context/view/contextList/", """{"contextList":"[Default Context, Test]"}"""));

        var result = await tools.GetContextListAsync(CancellationToken.None);

        Assert.StartsWith("Contexts (2):", result);
        Assert.Contains("Default Context", result);
    }

    [Fact]
    public async Task GetContextListAsync_Empty()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/context/view/contextList/", """{"contextList":"[]"}"""));

        var result = await tools.GetContextListAsync(CancellationToken.None);

        Assert.Equal("No contexts defined.", result);
    }

    [Fact]
    public async Task GetContextAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/context/view/context/",
                """{"context":{"id":"1","description":"test desc","includeRegexs":["http://example\\.com.*"],"excludeRegexs":[],"inScope":"true"}}"""));

        var result = await tools.GetContextAsync("TestCtx", CancellationToken.None);

        Assert.Contains("Context: TestCtx", result);
        Assert.Contains("ID: 1", result);
    }

    [Fact]
    public async Task CreateContextAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/context/action/newContext/", """{"contextId":"5"}"""));

        var result = await tools.CreateContextAsync("NewCtx", CancellationToken.None);

        Assert.Equal("Context 'NewCtx' created. Context ID: 5", result);
    }

    [Fact]
    public async Task RemoveContextAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/context/action/removeContext/", """{"Result":"OK"}"""));

        var result = await tools.RemoveContextAsync("OldCtx", CancellationToken.None);

        Assert.Equal("Context 'OldCtx' removed.", result);
    }

    [Fact]
    public async Task IncludeInContextAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/context/action/includeInContext/", """{"Result":"OK"}"""));

        var result = await tools.IncludeInContextAsync("Ctx", "http://example\\.com.*", CancellationToken.None);

        Assert.Contains("http://example\\.com.*", result);
        Assert.Contains("include list", result);
    }

    [Fact]
    public async Task ExcludeFromContextAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/context/action/excludeFromContext/", """{"Result":"OK"}"""));

        var result = await tools.ExcludeFromContextAsync("Ctx", "http://example\\.com/logout", CancellationToken.None);

        Assert.Contains("exclude list", result);
    }

    [Fact]
    public async Task ImportContextAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/context/action/importContext/", """{"contextId":"8"}"""));

        var result = await tools.ImportContextAsync("/zap/wrk/data/contexts/test.context", CancellationToken.None);

        Assert.Equal("Context imported. Context ID: 8", result);
    }

    [Fact]
    public async Task ExportContextAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/context/action/exportContext/", """{"Result":"OK"}"""));

        var result = await tools.ExportContextAsync("Ctx", "/zap/wrk/data/contexts/out.context", CancellationToken.None);

        Assert.Contains("exported to", result);
    }

    // -- Authentication --

    [Fact]
    public async Task GetAuthenticationMethodAsync_ReturnsJson()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/authentication/view/getAuthenticationMethod/",
                """{"method":{"methodName":"formBasedAuthentication"}}"""));

        var result = await tools.GetAuthenticationMethodAsync("1", CancellationToken.None);

        Assert.Contains("formBasedAuthentication", result);
    }

    [Fact]
    public async Task SetAuthenticationMethodAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/authentication/action/setAuthenticationMethod/", """{"Result":"OK"}"""));

        var result = await tools.SetAuthenticationMethodAsync("1", "formBasedAuthentication");

        Assert.Contains("formBasedAuthentication", result);
        Assert.Contains("context 1", result);
    }

    [Fact]
    public async Task SetLoggedInIndicatorAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/authentication/action/setLoggedInIndicator/", """{"Result":"OK"}"""));

        var result = await tools.SetLoggedInIndicatorAsync("1", "\\QLogout\\E", CancellationToken.None);

        Assert.Contains("Logged-in indicator set", result);
    }

    [Fact]
    public async Task SetLoggedOutIndicatorAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/authentication/action/setLoggedOutIndicator/", """{"Result":"OK"}"""));

        var result = await tools.SetLoggedOutIndicatorAsync("1", "\\QLogin\\E", CancellationToken.None);

        Assert.Contains("Logged-out indicator set", result);
    }

    // -- Users --

    [Fact]
    public async Task GetUsersListAsync_WithUsers()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/users/view/usersList/",
                """{"usersList":[{"id":"1","name":"admin","enabled":"true"}]}"""));

        var result = await tools.GetUsersListAsync("1", CancellationToken.None);

        Assert.Contains("admin", result);
        Assert.Contains("ID: 1", result);
    }

    [Fact]
    public async Task GetUsersListAsync_Empty()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/users/view/usersList/", """{"usersList":[]}"""));

        var result = await tools.GetUsersListAsync("1", CancellationToken.None);

        Assert.Equal("No users configured for this context.", result);
    }

    [Fact]
    public async Task CreateUserAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/users/action/newUser/", """{"userId":"10"}"""));

        var result = await tools.CreateUserAsync("1", "testuser", CancellationToken.None);

        Assert.Equal("User 'testuser' created. User ID: 10", result);
    }

    [Fact]
    public async Task RemoveUserAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/users/action/removeUser/", """{"Result":"OK"}"""));

        var result = await tools.RemoveUserAsync("1", "10", CancellationToken.None);

        Assert.Equal("User 10 removed from context 1.", result);
    }

    [Fact]
    public async Task SetAuthenticationCredentialsAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/users/action/setAuthenticationCredentials/", """{"Result":"OK"}"""));

        var result = await tools.SetAuthenticationCredentialsAsync("1", "10", "username=admin&password=secret", CancellationToken.None);

        Assert.Contains("Credentials set", result);
    }

    [Fact]
    public async Task SetUserEnabledAsync_Enable()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/users/action/setUserEnabled/", """{"Result":"OK"}"""));

        var result = await tools.SetUserEnabledAsync("1", "10", true, CancellationToken.None);

        Assert.Contains("enabled", result);
    }

    [Fact]
    public async Task SetUserEnabledAsync_Disable()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/users/action/setUserEnabled/", """{"Result":"OK"}"""));

        var result = await tools.SetUserEnabledAsync("1", "10", false, CancellationToken.None);

        Assert.Contains("disabled", result);
    }

    // -- Forced User --

    [Fact]
    public async Task SetForcedUserAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/forcedUser/action/setForcedUser/", """{"Result":"OK"}"""));

        var result = await tools.SetForcedUserAsync("1", "10", CancellationToken.None);

        Assert.Equal("Forced user set to 10 for context 1.", result);
    }

    [Fact]
    public async Task SetForcedUserModeEnabledAsync_Enable()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/forcedUser/action/setForcedUserModeEnabled/", """{"Result":"OK"}"""));

        var result = await tools.SetForcedUserModeEnabledAsync(true, CancellationToken.None);

        Assert.Equal("Forced user mode enabled.", result);
    }

    [Fact]
    public async Task GetForcedUserStatusAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/forcedUser/view/isForcedUserModeEnabled/", """{"forcedModeEnabled":"false"}""")
             .When("/JSON/forcedUser/view/getForcedUser/", """{"forcedUserId":""}"""));

        var result = await tools.GetForcedUserStatusAsync("1", CancellationToken.None);

        Assert.Contains("Mode enabled: False", result);
        Assert.Contains("(none)", result);
    }

    // -- Ajax Spider --

    [Fact]
    public async Task StartAjaxSpiderAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/ajaxSpider/action/scan/", """{"Result":"OK"}"""));

        var result = await tools.StartAjaxSpiderAsync("http://example.com");

        Assert.Equal("Ajax Spider started. Result: OK", result);
    }

    [Fact]
    public async Task StartAjaxSpiderAsUserAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/ajaxSpider/action/scanAsUser/", """{"Result":"OK"}"""));

        var result = await tools.StartAjaxSpiderAsUserAsync("Ctx", "10", "http://example.com");

        Assert.Contains("as user 10", result);
    }

    [Fact]
    public async Task GetAjaxSpiderStatusAsync_Running()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/ajaxSpider/view/status/", """{"status":"running"}"""));

        var result = await tools.GetAjaxSpiderStatusAsync(CancellationToken.None);

        Assert.Equal("Ajax Spider is running.", result);
    }

    [Fact]
    public async Task GetAjaxSpiderStatusAsync_Stopped()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/ajaxSpider/view/status/", """{"status":"stopped"}"""));

        var result = await tools.GetAjaxSpiderStatusAsync(CancellationToken.None);

        Assert.Equal("Ajax Spider status: stopped", result);
    }

    [Fact]
    public async Task GetAjaxSpiderResultsAsync_Zero()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/ajaxSpider/view/numberOfResults/", """{"numberOfResults":"0"}"""));

        var result = await tools.GetAjaxSpiderResultsAsync(CancellationToken.None);

        Assert.Equal("Ajax Spider has found 0 results.", result);
    }

    [Fact]
    public async Task GetAjaxSpiderResultsAsync_WithResults()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/ajaxSpider/view/numberOfResults/", """{"numberOfResults":"2"}""")
             .When("/JSON/ajaxSpider/view/fullResults/",
                """{"fullResults":[{"requestHeader":"GET http://example.com/ HTTP/1.1\nHost: example.com"},{"requestHeader":"GET http://example.com/page HTTP/1.1\nHost: example.com"}]}"""));

        var result = await tools.GetAjaxSpiderResultsAsync(CancellationToken.None);

        Assert.Contains("2 found", result);
        Assert.Contains("GET http://example.com/", result);
    }

    [Fact]
    public async Task StopAjaxSpiderAsync_FormatsOutput()
    {
        var tools = CreateTools(h =>
            h.When("/JSON/ajaxSpider/action/stop/", """{"Result":"OK"}"""));

        var result = await tools.StopAjaxSpiderAsync(CancellationToken.None);

        Assert.Equal("Ajax Spider stopped.", result);
    }
}
