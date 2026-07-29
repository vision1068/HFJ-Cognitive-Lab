using System.Net;

namespace GoldSignalAnalyzer.Tests.Support;

/// <summary>
/// Test double for HttpClient: routes by the request's absolute path and
/// records every request so tests can assert on headers (e.g. the auth token).
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _routes = new();
    public List<HttpRequestMessage> Requests { get; } = new();

    public StubHttpMessageHandler Map(string path, HttpStatusCode status, string? json)
    {
        _routes[path] = _ =>
        {
            var resp = new HttpResponseMessage(status);
            if (json is not null)
                resp.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            return resp;
        };
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        var path = request.RequestUri!.AbsolutePath.TrimEnd('/');
        if (_routes.TryGetValue(path, out var handler))
            return Task.FromResult(handler(request));
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
