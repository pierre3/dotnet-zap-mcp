using System.ComponentModel;
using ModelContextProtocol.Server;

namespace ZapMcpServer;

/// <summary>
/// MCP tools for managing the ZAP Docker container via Docker Compose.
/// </summary>
[McpServerToolType]
internal sealed class DockerComposeTools
{
    private readonly DockerComposeManager _docker;
    private readonly ZapApiClient _zap;

    public DockerComposeTools(DockerComposeManager docker, ZapApiClient zap)
    {
        _docker = docker;
        _zap = zap;
    }

    [McpServerTool, Description("Start the OWASP ZAP Docker container. Extracts Docker assets, generates config, starts the container, and waits for ZAP to be healthy.")]
    public async Task<string> DockerComposeUpAsync(CancellationToken ct)
    {
        _docker.EnsureAssetsExtracted();
        var (baseUrl, apiKey) = _docker.EnsureEnvFile();

        var (exitCode, stdout, stderr) = await _docker.RunDockerComposeAsync("up -d", ct);
        if (exitCode != 0)
        {
            return $"Failed to start ZAP container (exit code {exitCode}).\nstdout: {stdout}\nstderr: {stderr}";
        }

        var healthy = await _docker.WaitForHealthyAsync(baseUrl, apiKey, ct: ct);
        if (!healthy)
        {
            return $"ZAP container started but did not become healthy within 120 seconds.\nTry 'DockerComposeLogs' to check container logs.\nstdout: {stdout}\nstderr: {stderr}";
        }

        _zap.Reconfigure(baseUrl, apiKey);

        return $"ZAP container started and healthy.\nBase URL: {baseUrl}\nAPI Key: {apiKey}\nAll ZAP scanning tools are now available.";
    }

    [McpServerTool, Description("Stop and remove the OWASP ZAP Docker container.")]
    public async Task<string> DockerComposeDownAsync(CancellationToken ct)
    {
        var (exitCode, stdout, stderr) = await _docker.RunDockerComposeAsync("down", ct);
        if (exitCode != 0)
        {
            return $"Failed to stop ZAP container (exit code {exitCode}).\nstdout: {stdout}\nstderr: {stderr}";
        }
        return "ZAP container stopped and removed.";
    }

    [McpServerTool, Description("Check the status of the OWASP ZAP Docker container.")]
    public async Task<string> DockerComposeStatusAsync(CancellationToken ct)
    {
        var (exitCode, stdout, stderr) = await _docker.RunDockerComposeAsync("ps", ct);
        if (exitCode != 0)
        {
            return $"Failed to get container status (exit code {exitCode}).\nstderr: {stderr}";
        }
        return string.IsNullOrWhiteSpace(stdout)
            ? "No ZAP containers running."
            : stdout;
    }

    [McpServerTool, Description("Get recent logs from the OWASP ZAP Docker container.")]
    public async Task<string> DockerComposeLogsAsync(
        [Description("Number of log lines to return (default 50)")] int tail = 50,
        CancellationToken ct = default)
    {
        var (exitCode, stdout, stderr) = await _docker.RunDockerComposeAsync($"logs --tail {tail}", ct);
        if (exitCode != 0)
        {
            return $"Failed to get container logs (exit code {exitCode}).\nstderr: {stderr}";
        }
        return string.IsNullOrWhiteSpace(stdout)
            ? "No logs available."
            : stdout;
    }
}
