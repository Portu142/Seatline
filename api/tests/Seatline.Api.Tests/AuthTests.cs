using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Seatline.Api.Auth;
using Seatline.Api.Tests.Infrastructure;

namespace Seatline.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class AuthTests(SeatlineApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";

    [Fact]
    public async Task Register_creates_a_customer_and_returns_tokens()
    {
        var email = NewEmail();

        var response = await _client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest(email, "Password123", "Ada Lovelace"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
        Assert.Equal(email, auth.User.Email);
        Assert.Equal([Roles.Customer], auth.User.Roles);
    }

    [Fact]
    public async Task Register_with_an_existing_email_returns_409()
    {
        var email = NewEmail();
        var request = new RegisterRequest(email, "Password123", "First");
        (await _client.PostAsJsonAsync("/api/auth/register", request, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        var second = await _client.PostAsJsonAsync("/api/auth/register", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Theory]
    [InlineData("not-an-email", "Password123", "Name")]
    [InlineData("valid@example.com", "short", "Name")]
    [InlineData("valid@example.com", "Password123", "")]
    public async Task Register_rejects_invalid_input_with_400(string email, string password, string displayName)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest(email, password, displayName), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Registered_user_can_log_in_and_read_their_profile()
    {
        var email = NewEmail();
        (await _client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest(email, "Password123", "Grace Hopper"), TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        var login = await _client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest(email, "Password123"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = (await login.Content.ReadFromJsonAsync<AuthResponse>(TestContext.Current.CancellationToken))!;

        using var me = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        me.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var profile = await _client.SendAsync(me, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        var user = await profile.Content.ReadFromJsonAsync<UserDto>(TestContext.Current.CancellationToken);
        Assert.Equal("Grace Hopper", user!.DisplayName);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest("customer@seatline.dev", "WrongPassword1"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_unknown_email_returns_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest(NewEmail(), "Password123"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_without_a_token_returns_401()
    {
        var response = await _client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_with_a_tampered_token_returns_401()
    {
        var auth = await factory.LoginAsync("customer@seatline.dev");
        var tampered = auth.AccessToken[..^4] + (auth.AccessToken.EndsWith("AAAA", StringComparison.Ordinal) ? "BBBB" : "AAAA");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tampered);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_the_old_one_stops_working()
    {
        var email = NewEmail();
        var register = await _client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest(email, "Password123", "Rotating"), TestContext.Current.CancellationToken);
        var first = (await register.Content.ReadFromJsonAsync<AuthResponse>(TestContext.Current.CancellationToken))!;

        var refreshed = await _client.PostAsJsonAsync(
            "/api/auth/refresh", new RefreshRequest(first.RefreshToken), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var second = (await refreshed.Content.ReadFromJsonAsync<AuthResponse>(TestContext.Current.CancellationToken))!;
        Assert.NotEqual(first.RefreshToken, second.RefreshToken);

        var reused = await _client.PostAsJsonAsync(
            "/api/auth/refresh", new RefreshRequest(first.RefreshToken), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);

        // Reusing a rotated token looks like theft, so the whole token family is revoked.
        var afterTheft = await _client.PostAsJsonAsync(
            "/api/auth/refresh", new RefreshRequest(second.RefreshToken), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, afterTheft.StatusCode);
    }

    [Fact]
    public async Task Logout_revokes_the_refresh_token()
    {
        var auth = await factory.LoginAsync("validator@seatline.dev");

        var logout = await _client.PostAsJsonAsync(
            "/api/auth/logout", new RefreshRequest(auth.RefreshToken), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var refresh = await _client.PostAsJsonAsync(
            "/api/auth/refresh", new RefreshRequest(auth.RefreshToken), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }
}
