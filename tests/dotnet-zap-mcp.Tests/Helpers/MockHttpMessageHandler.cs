using System.Net;
using System.Text;

namespace ZapMcpServer.Tests.Helpers;

/// <summary>
/// A mock HttpMessageHandler that returns pre-configured responses based on URL patterns.
/// </summary>
internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly List<(Func<string, bool> Predicate, Func<HttpRequestMessage, HttpResponseMessage> ResponseFactory)> _rules = [];
    private HttpResponseMessage _defaultResponse = new(HttpStatusCode.NotFound)
    {
        Content = new StringContent("{\"error\":\"not found\"}", Encoding.UTF8, "application/json")
    };

    /// <summary>
    /// Register a rule that matches when the request URL contains the given substring.
    /// </summary>
    public MockHttpMessageHandler When(string urlContains, string jsonResponse)
    {
        _rules.Add((
            url => url.Contains(urlContains, StringComparison.OrdinalIgnoreCase),
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            }
        ));
        return this;
    }

    /// <summary>
    /// Register a rule that matches when the request URL contains the given substring and returns raw bytes.
    /// </summary>
    public MockHttpMessageHandler WhenBytes(string urlContains, byte[] responseBytes, string contentType = "application/octet-stream")
    {
        _rules.Add((
            url => url.Contains(urlContains, StringComparison.OrdinalIgnoreCase),
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(responseBytes)
                {
                    Headers = { { "Content-Type", contentType } }
                }
            }
        ));
        return this;
    }

    /// <summary>
    /// Register a rule that matches when the request URL contains the given substring and returns a specific status code.
    /// </summary>
    public MockHttpMessageHandler WhenStatus(string urlContains, HttpStatusCode statusCode)
    {
        _rules.Add((
            url => url.Contains(urlContains, StringComparison.OrdinalIgnoreCase),
            _ => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            }
        ));
        return this;
    }

    /// <summary>
    /// Captured requests for assertion purposes.
    /// </summary>
    public List<HttpRequestMessage> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        var url = request.RequestUri?.ToString() ?? "";

        foreach (var (predicate, factory) in _rules)
        {
            if (predicate(url))
                return Task.FromResult(factory(request));
        }

        return Task.FromResult(_defaultResponse);
    }
}
