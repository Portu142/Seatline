using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Seatline.Api.Auth;

namespace Seatline.Api.Data;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>Create one demo account per role (shown in the README so reviewers can try the app).</summary>
    public bool DemoUsers { get; init; }

    public string DemoPassword { get; init; } = string.Empty;
}

/// <summary>Applies pending EF Core migrations and seeds roles and demo users. Safe to run on every start.</summary>
public static partial class DatabaseInitializer
{
    public static readonly IReadOnlyList<(string Email, string DisplayName, string Role)> DemoAccounts =
    [
        ("customer@seatline.dev", "Demo Customer", Roles.Customer),
        ("admin@seatline.dev", "Demo Box Office", Roles.Admin),
        ("validator@seatline.dev", "Demo Door Staff", Roles.Validator),
    ];

    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var config = sp.GetRequiredService<IConfiguration>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));

        if (config.GetValue("Database:MigrateOnStartup", true))
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync(ct);
            LogMigrated(logger);
        }

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var seed = config.GetSection(SeedOptions.SectionName).Get<SeedOptions>() ?? new SeedOptions();
        if (!seed.DemoUsers)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(seed.DemoPassword))
        {
            throw new InvalidOperationException("Seed:DemoUsers is enabled but Seed:DemoPassword is empty.");
        }

        var userManager = sp.GetRequiredService<UserManager<AppUser>>();
        var clock = sp.GetRequiredService<TimeProvider>();
        foreach (var (email, displayName, role) in DemoAccounts)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
            {
                continue;
            }

            var user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                CreatedAt = clock.GetUtcNow(),
            };

            var result = await userManager.CreateAsync(user, seed.DemoPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not seed {email}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
            }

            await userManager.AddToRoleAsync(user, role);
            LogSeededUser(logger, email, role);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Database migrations applied")]
    private static partial void LogMigrated(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded demo user {Email} with role {Role}")]
    private static partial void LogSeededUser(ILogger logger, string email, string role);
}
