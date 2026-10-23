using System.Net;
using System.Text.Json;
using Asuka.Core;
using Xunit;
namespace Asuka.Tests;

public class PresenceTests
{
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> run) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => run(request);
    }
    [Fact] public async Task SendsOnlyPresenceAndBearerToken()
    {
        using var http = new HttpClient(new Handler(async request =>
        {
            Assert.Equal("http://192.168.1.26:8765/api/presence", request.RequestUri!.AbsoluteUri);
            Assert.Equal("Bearer test-only", request.Headers.Authorization!.ToString());
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            Assert.Equal(4, json.RootElement.EnumerateObject().Count());
            Assert.Equal("Cuisine", json.RootElement.GetProperty("piece").GetString());
            Assert.True(json.RootElement.GetProperty("teddy").GetBoolean());
            Assert.True(json.RootElement.GetProperty("heure").TryGetDateTimeOffset(out _));
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }));
        Assert.True(await new PresenceClient(http).SendAsync(new(), true, "test-only"));
    }
    [Theory][InlineData(404)][InlineData(401)][InlineData(500)]
    public async Task ServerFailureIsIgnored(int status)
    {
        using var http = new HttpClient(new Handler(_ => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status))));
        Assert.False(await new PresenceClient(http).SendAsync(new(), false, "test-only"));
    }
    [Fact] public async Task NetworkErrorIsIgnored()
    {
        using var http = new HttpClient(new Handler(_ => throw new HttpRequestException("offline")));
        Assert.False(await new PresenceClient(http).SendAsync(new(), false, "test-only"));
    }
    [Fact] public async Task TimeoutIsIgnored()
    {
        using var http = new HttpClient(new Handler(_ => throw new TaskCanceledException()));
        Assert.False(await new PresenceClient(http).SendAsync(new(), false, "test-only"));
    }
    [Fact] public async Task NoTokenMeansNoRequest()
    {
        using var http = new HttpClient(new Handler(_ => throw new InvalidOperationException("No request expected")));
        Assert.False(await new PresenceClient(http).SendAsync(new(), false, ""));
    }
}
