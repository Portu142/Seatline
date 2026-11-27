using System.Net;
using System.Net.Http.Json;
using Seatline.Api.Auth;
using Seatline.Api.Tests.Infrastructure;

namespace Seatline.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class RoleAccessTests(SeatlineApiFactory factory)
{
    [Theory]
    [InlineData("customer@seatline.dev", Roles.Customer)]
    [InlineData("admin@seatline.dev", Roles.Admin)]
    [InlineData("validator@seatline.dev", Roles.Validator)]
    public async Task Demo_users_are_seeded_with_their_role(string email, string role)
    {
        var auth = await factory.LoginAsync(email);

        Assert.Equal([role], auth.User.Roles);
    }

    [Theory]
    [InlineData("admin@seatline.dev", HttpStatusCode.OK)]
    [InlineData("customer@seatline.dev", HttpStatusCode.Forbidden)]
    [InlineData("validator@seatline.dev", HttpStatusCode.Forbidden)]
    public async Task Admin_area_is_only_for_admins(string email, HttpStatusCode expected)
    {
        using var client = await factory.CreateClientAsAsync(email);

        var response = await client.GetAsync("/api/admin/overview", TestContext.Current.CancellationToken);

        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData("validator@seatline.dev", HttpStatusCode.OK)]
    [InlineData("admin@seatline.dev", HttpStatusCode.OK)]
    [InlineData("customer@seatline.dev", HttpStatusCode.Forbidden)]
    public async Task Door_area_is_for_validators_and_admins(string email, HttpStatusCode expected)
    {
        using var client = await factory.CreateClientAsAsync(email);

        var response = await client.GetAsync("/api/door/status", TestContext.Current.CancellationToken);

        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/admin/overview")]
    [InlineData("/api/door/status")]
    public async Task Protected_areas_require_a_token(string url)
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(url, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Newly_registered_users_cannot_reach_the_admin_area()
    {
        using var client = factory.CreateClient();
        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest($"sneaky-{Guid.NewGuid():N}@example.com", "Password123", "Sneaky"),
            TestContext.Current.CancellationToken);
        var auth = (await register.Content.ReadFromJsonAsync<AuthResponse>(TestContext.Current.CancellationToken))!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth.AccessToken);

        var response = await client.GetAsync("/api/admin/overview", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
