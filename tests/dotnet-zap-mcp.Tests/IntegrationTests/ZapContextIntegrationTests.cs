using Xunit;

namespace ZapMcpServer.Tests.IntegrationTests;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class ZapContextIntegrationTests
{
    private readonly ZapApiClient _zap;

    public ZapContextIntegrationTests(ZapIntegrationFixture fixture)
    {
        _zap = fixture.ZapClient;
    }

    [Fact]
    public async Task ContextCrud_CreateListGetRemove()
    {
        var ctxName = $"TestCtx_{Guid.NewGuid():N}";

        // Create
        var ctxId = await _zap.CreateContextAsync(ctxName);
        Assert.False(string.IsNullOrEmpty(ctxId));

        // List
        var list = await _zap.GetContextListAsync();
        Assert.Contains(ctxName, list);

        // Get details
        var details = await _zap.GetContextAsync(ctxName);
        Assert.Equal(ctxId, details.GetProperty("context").GetProperty("id").GetString());

        // Include pattern
        var includeResult = await _zap.IncludeInContextAsync(ctxName, "http://target/.*");
        Assert.Equal("OK", includeResult);

        // Exclude pattern
        var excludeResult = await _zap.ExcludeFromContextAsync(ctxName, "http://target/logout");
        Assert.Equal("OK", excludeResult);

        // Remove
        var removeResult = await _zap.RemoveContextAsync(ctxName);
        Assert.Equal("OK", removeResult);

        // Verify removed
        var listAfter = await _zap.GetContextListAsync();
        Assert.DoesNotContain(ctxName, listAfter);
    }

    [Fact]
    public async Task ContextExportImport_RoundTrip()
    {
        var ctxName = $"ExportCtx_{Guid.NewGuid().ToString("N")[..8]}";
        var exportPath = $"/zap/wrk/data/contexts/{ctxName}.context";

        // Create and configure
        var ctxId = await _zap.CreateContextAsync(ctxName);
        await _zap.IncludeInContextAsync(ctxName, "http://target/.*");

        // Export
        var exportResult = await _zap.ExportContextAsync(ctxName, exportPath);
        Assert.Equal("OK", exportResult);

        // Remove original
        await _zap.RemoveContextAsync(ctxName);

        // Import
        var importedId = await _zap.ImportContextAsync(exportPath);
        Assert.False(string.IsNullOrEmpty(importedId));

        // Cleanup
        await _zap.RemoveContextAsync(ctxName);
    }

    [Fact]
    public async Task Authentication_SetAndGet()
    {
        var ctxName = $"AuthCtx_{Guid.NewGuid():N}";
        var ctxId = await _zap.CreateContextAsync(ctxName);

        try
        {
            // Set authentication method
            var setResult = await _zap.SetAuthenticationMethodAsync(
                ctxId, "formBasedAuthentication",
                "loginUrl=http://target/login&loginRequestData=username%3D%7B%25username%25%7D%26password%3D%7B%25password%25%7D");
            Assert.Equal("OK", setResult);

            // Get authentication method
            var authMethod = await _zap.GetAuthenticationMethodAsync(ctxId);
            Assert.Contains("formBasedAuthentication",
                authMethod.GetProperty("method").GetProperty("methodName").GetString());

            // Set logged-in indicator
            var loggedInResult = await _zap.SetLoggedInIndicatorAsync(ctxId, "\\QLogout\\E");
            Assert.Equal("OK", loggedInResult);

            // Set logged-out indicator
            var loggedOutResult = await _zap.SetLoggedOutIndicatorAsync(ctxId, "\\QLogin\\E");
            Assert.Equal("OK", loggedOutResult);
        }
        finally
        {
            await _zap.RemoveContextAsync(ctxName);
        }
    }

    [Fact]
    public async Task UserCrud_CreateGetSetRemove()
    {
        var ctxName = $"UserCtx_{Guid.NewGuid():N}";
        var ctxId = await _zap.CreateContextAsync(ctxName);

        try
        {
            // Set auth method first (required for credential setting)
            await _zap.SetAuthenticationMethodAsync(
                ctxId, "formBasedAuthentication",
                "loginUrl=http://target/login&loginRequestData=username%3D%7B%25username%25%7D%26password%3D%7B%25password%25%7D");

            // Create user
            var userId = await _zap.CreateUserAsync(ctxId, "testuser");
            Assert.False(string.IsNullOrEmpty(userId));

            // Get users list
            var users = await _zap.GetUsersListAsync(ctxId);
            var usersList = users.GetProperty("usersList");
            Assert.True(usersList.GetArrayLength() > 0);

            // Set credentials
            var credResult = await _zap.SetAuthenticationCredentialsAsync(
                ctxId, userId, "username=admin&password=password");
            Assert.Equal("OK", credResult);

            // Enable user
            var enableResult = await _zap.SetUserEnabledAsync(ctxId, userId, true);
            Assert.Equal("OK", enableResult);

            // Disable user
            var disableResult = await _zap.SetUserEnabledAsync(ctxId, userId, false);
            Assert.Equal("OK", disableResult);

            // Remove user
            var removeResult = await _zap.RemoveUserAsync(ctxId, userId);
            Assert.Equal("OK", removeResult);
        }
        finally
        {
            await _zap.RemoveContextAsync(ctxName);
        }
    }

    [Fact]
    public async Task ForcedUser_SetAndGet()
    {
        var ctxName = $"ForcedCtx_{Guid.NewGuid():N}";
        var ctxId = await _zap.CreateContextAsync(ctxName);

        try
        {
            await _zap.SetAuthenticationMethodAsync(
                ctxId, "formBasedAuthentication",
                "loginUrl=http://target/login&loginRequestData=username%3D%7B%25username%25%7D%26password%3D%7B%25password%25%7D");

            var userId = await _zap.CreateUserAsync(ctxId, "forceduser");
            await _zap.SetAuthenticationCredentialsAsync(ctxId, userId, "username=admin&password=password");
            await _zap.SetUserEnabledAsync(ctxId, userId, true);

            // Set forced user
            var setResult = await _zap.SetForcedUserAsync(ctxId, userId);
            Assert.Equal("OK", setResult);

            // Get forced user
            var forcedUserId = await _zap.GetForcedUserAsync(ctxId);
            Assert.Equal(userId, forcedUserId);

            // Enable forced user mode
            var modeResult = await _zap.SetForcedUserModeEnabledAsync(true);
            Assert.Equal("OK", modeResult);

            // Check mode is enabled
            var modeEnabled = await _zap.IsForcedUserModeEnabledAsync();
            Assert.True(modeEnabled);

            // Disable forced user mode
            await _zap.SetForcedUserModeEnabledAsync(false);
        }
        finally
        {
            await _zap.RemoveContextAsync(ctxName);
        }
    }
}
