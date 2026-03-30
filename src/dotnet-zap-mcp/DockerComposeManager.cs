using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;

namespace ZapMcpServer;

/// <summary>
/// Manages Docker Compose assets extraction and process execution for the ZAP container.
/// </summary>
internal sealed class DockerComposeManager
{
    private static readonly string[] ResourceNames = ["docker-compose.yml", "entrypoint.sh", "zap-config.xml"];

    private readonly string _dockerDir;
    private readonly string _version;

    /// <summary>
    /// The container-internal path for shared data (reports, sessions, contexts).
    /// Use this when constructing file paths for ZAP API calls that reference the container filesystem.
    /// </summary>
    public const string ContainerDataPath = "/zap/wrk/data";

    /// <summary>
    /// Subdirectory names under <see cref="ContainerDataPath"/>.
    /// </summary>
    public const string ReportsDir = "reports";
    public const string SessionsDir = "sessions";
    public const string ContextsDir = "contexts";

    public DockerComposeManager()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _dockerDir = Path.Combine(home, ".zap-mcp", "docker");
        _version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
    }

    /// <summary>
    /// Returns the host-side path of the shared data volume directory.
    /// Files written by ZAP to <see cref="ContainerDataPath"/> inside the container
    /// can be read from this path on the host (when using a named volume, use
    /// <c>docker cp</c> or inspect the volume mount point).
    /// </summary>
    public string GetDataDirectory() => Path.Combine(_dockerDir, "data");

    /// <summary>
    /// Returns the path to the Docker directory, extracting embedded resources if needed.
    /// </summary>
    public string EnsureAssetsExtracted()
    {
        Directory.CreateDirectory(_dockerDir);

        var versionFile = Path.Combine(_dockerDir, ".version");
        if (File.Exists(versionFile) && File.ReadAllText(versionFile).Trim() == _version)
        {
            return _dockerDir;
        }

        var assembly = Assembly.GetExecutingAssembly();
        foreach (var name in ResourceNames)
        {
            using var stream = assembly.GetManifestResourceStream(name)
                ?? throw new InvalidOperationException($"Embedded resource '{name}' not found.");
            var destPath = Path.Combine(_dockerDir, name);
            using var fs = File.Create(destPath);
            stream.CopyTo(fs);
        }

        File.WriteAllText(versionFile, _version);
        return _dockerDir;
    }

    /// <summary>
    /// Ensures the .env file exists with the API key and base URL.
    /// If ZAP_API_KEY is not set, generates a random key.
    /// Returns (baseUrl, apiKey).
    /// </summary>
    public (string BaseUrl, string ApiKey) EnsureEnvFile()
    {
        var apiKey = Environment.GetEnvironmentVariable("ZAP_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            apiKey = RandomNumberGenerator.GetHexString(32, lowercase: true);
        }

        var baseUrl = Environment.GetEnvironmentVariable("ZAP_BASE_URL") ?? "http://127.0.0.1:8090";

        var envPath = Path.Combine(_dockerDir, ".env");
        File.WriteAllText(envPath, $"ZAP_API_KEY={apiKey}\nZAP_BASE_URL={baseUrl}\n");

        return (baseUrl, apiKey);
    }

    /// <summary>
    /// Run a docker compose command and return (exitCode, stdout, stderr).
    /// </summary>
    public async Task<(int ExitCode, string StdOut, string StdErr)> RunDockerComposeAsync(
        string arguments, CancellationToken ct = default)
    {
        var envFile = Path.Combine(_dockerDir, ".env");
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"compose --project-directory \"{_dockerDir}\" --env-file \"{envFile}\" {arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = psi };
        try
        {
            process.Start();
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return (-1, "", "Docker is not installed or not found in PATH. Please install Docker Desktop and ensure 'docker' is available.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return (process.ExitCode, stdout, stderr);
    }

    /// <summary>
    /// Poll the ZAP version endpoint until it responds successfully.
    /// </summary>
    public async Task<bool> WaitForHealthyAsync(string baseUrl, string apiKey, int timeoutSeconds = 120, CancellationToken ct = default)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var response = await http.GetAsync(
                    $"{baseUrl}/JSON/core/view/version/?apikey={apiKey}", ct);
                if (response.IsSuccessStatusCode)
                    return true;
            }
            catch
            {
                // Not ready yet
            }
            await Task.Delay(5000, ct);
        }

        return false;
    }
}
