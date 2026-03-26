using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ZapMcpServer;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(_ =>
{
    var baseUrl = Environment.GetEnvironmentVariable("ZAP_BASE_URL") ?? "http://localhost:8090";
    var apiKey = Environment.GetEnvironmentVariable("ZAP_API_KEY") ?? "";
    return new ZapApiClient(baseUrl, apiKey);
});

builder.Services.AddSingleton<DockerComposeManager>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
