using System.Net;
using System.Text;

namespace Offroad.IntegrationTests.Infrastructure;

/// <summary>
/// Replaces the real HTTP primary handler for GraphHopper.
/// Sits behind the Polly resilience pipeline — retries and timeouts still apply.
/// </summary>
public sealed class MockGraphHopperHandler : HttpMessageHandler
{
    private Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responseFactory
        = (_, _) => throw new InvalidOperationException(
            "MockGraphHopperHandler not configured — call SetupJsonResponse or SetupDelay first.");

    public List<CapturedGraphHopperRequest> CapturedRequests { get; } = new();

    public void SetupJsonResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responseFactory = (_, _) => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
    }

    public void SetupDelay(TimeSpan delay)
    {
        _responseFactory = async (_, ct) =>
        {
            await Task.Delay(delay, ct);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        };
    }

    public void Reset()
    {
        CapturedRequests.Clear();
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? body = request.Content is not null
            ? await request.Content.ReadAsStringAsync(cancellationToken)
            : null;

        CapturedRequests.Add(new CapturedGraphHopperRequest(request.RequestUri, request.Method, body));

        return await _responseFactory(request, cancellationToken);
    }
}

public sealed record CapturedGraphHopperRequest(Uri? Uri, HttpMethod Method, string? Body);
