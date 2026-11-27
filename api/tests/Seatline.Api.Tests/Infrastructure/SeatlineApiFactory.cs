using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Seatline.Api.Auth;
using Testcontainers.PostgreSql;

namespace Seatline.Api.Tests.Infrastructure;

/// <summary>
/// Boots the real API in memory against a real PostgreSQL started by Testcontainers.
/// One container is shared by every test in the "api" collection.
/// </summary>
public sealed class SeatlineApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string DemoPassword = "Seatline2026!";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("seatline_tests")
        .Build();

    public async ValueTask InitializeAsync() => await _postgres.StartAsync();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-0123456789abcdef");
        builder.UseSetting("Seed:DemoUsers", "true");
        builder.UseSetting("Seed:DemoPassword", DemoPassword);
        builder.UseSetting("RateLimiting:AuthRequestsPerMinute", "10000");
    }

    public async Task<AuthResponse> LoginAsync(string email, string password = DemoPassword)
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    public async Task<HttpClient> CreateClientAsAsync(string email)
    {
        var auth = await LoginAsync(email);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<SeatlineApiFactory>
{
    public const string Name = "api";
}
