using System.Diagnostics;
using Xunit;

namespace ZapMcpServer.Tests.IntegrationTests;

/// <summary>
/// Shared fixture that manages the test Docker environment (ZAP + target app).
/// Implements IAsyncLifetime to start containers before tests and stop them after.
/// </summary>
public class ZapIntegrationFixture : IAsyncLifetime
{
    public const string ZapBaseUrl = "http://127.0.0.1:8090";
    public const string ApiKey = "test-api-key-for-ci";

    /// <summary>
    /// The target URL as seen from inside the ZAP container (Docker network DNS).
    /// </summary>
    public const string TargetUrl = "http://target";

    private string ComposeFilePath => FindComposeFile();

    internal ZapApiClient ZapClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Start containers (skip if already running — CI may pre-start them)
        var (exitCode, _, stderr) = await RunDockerComposeAsync("ps --format json");
        if (exitCode != 0 || string.IsNullOrWhiteSpace(stderr) is false && stderr.Contains("no such service"))
        {
            await RunDockerComposeAsync("up -d --build");
        }
        else
        {
            // Check if containers are already running
            var (_, stdout, _) = await RunDockerComposeAsync("ps --status running --format {{.Name}}");
            if (!stdout.Contains("zap-test") || !stdout.Contains("target-test"))
            {
                await RunDockerComposeAsync("up -d --build");
            }
        }

        // Wait for ZAP to become healthy
        await WaitForHealthyAsync(
            $"{ZapBaseUrl}/JSON/core/view/version/?apikey={ApiKey}",
            timeoutSeconds: 180);

        // Wait for target app to become healthy
        await WaitForHealthyAsync(
            "http://127.0.0.1:8080/health",
            timeoutSeconds: 60);

        ZapClient = new ZapApiClient(ZapBaseUrl, ApiKey);
    }

    public async Task DisposeAsync()
    {
        // CI handles cleanup in a separate step; locally, keep containers running for re-runs.
        // Set INTEGRATION_TEST_CLEANUP=true to tear down after tests.
        if (Environment.GetEnvironmentVariable("INTEGRATION_TEST_CLEANUP") == "true")
        {
            await RunDockerComposeAsync("down -v");
        }
    }

    private async Task<(int ExitCode, string StdOut, string StdErr)> RunDockerComposeAsync(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"compose -f \"{ComposeFilePath}\" {arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, stdout, stderr);
    }

    private static async Task WaitForHealthyAsync(string url, int timeoutSeconds)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await http.GetAsync(url);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch
            {
                // Not ready yet
            }
            await Task.Delay(3000);
        }

        throw new TimeoutException($"Service at {url} did not become healthy within {timeoutSeconds}s");
    }

    private static string FindComposeFile()
    {
        // Walk up from the test assembly location to find the compose file
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(dir, "tests", "docker", "docker-compose.test.yml");
            if (File.Exists(candidate))
                return candidate;

            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }

        // Fallback: try relative to working directory
        var fallback = Path.GetFullPath("tests/docker/docker-compose.test.yml");
        if (File.Exists(fallback))
            return fallback;

        throw new FileNotFoundException("Could not find tests/docker/docker-compose.test.yml");
    }
}

[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<ZapIntegrationFixture>;
