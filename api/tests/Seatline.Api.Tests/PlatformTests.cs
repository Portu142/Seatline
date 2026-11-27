using System.Net;
using System.Text.Json;
using Seatline.Api.Tests.Infrastructure;

namespace Seatline.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class PlatformTests(SeatlineApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_reports_healthy_when_the_database_is_up()
    {
        var response = await _client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task OpenApi_document_is_published_with_the_bearer_scheme()
    {
        var response = await _client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("/api/auth/login", json, StringComparison.Ordinal);
        Assert.Contains("\"bearer\"", json, StringComparison.Ordinal);
    }
}
