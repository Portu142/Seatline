using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Seatline.Api.Data;

namespace Seatline.Api.Auth;

public sealed class TokenService(
    AppDbContext db,
    UserManager<AppUser> userManager,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider clock)
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    /// <summary>Issues a fresh access token + refresh token pair for the user.</summary>
    public async Task<AuthResponse> IssueAsync(AppUser user, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var roles = await userManager.GetRolesAsync(user);

        var (refreshToken, refreshEntity) = NewRefreshToken(user.Id, now);
        db.RefreshTokens.Add(refreshEntity);
        await db.SaveChangesAsync(ct);

        return BuildResponse(user, roles, now, refreshToken, refreshEntity.ExpiresAt);
    }

    /// <summary>
    /// Exchanges a refresh token for a new pair (rotation). Presenting a token that was already
    /// rotated is treated as theft: every active refresh token of that user is revoked.
    /// </summary>
    public async Task<AuthResponse?> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var hash = Hash(refreshToken);

        var stored = await db.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null)
        {
            return null;
        }

        if (stored.RevokedAt is not null)
        {
            await RevokeAllAsync(stored.UserId, now, ct);
            return null;
        }

        if (stored.ExpiresAt <= now || stored.User.LockoutEnd > now)
        {
            return null;
        }

        var (newToken, newEntity) = NewRefreshToken(stored.UserId, now);
        stored.RevokedAt = now;
        stored.ReplacedByTokenHash = newEntity.TokenHash;
        db.RefreshTokens.Add(newEntity);
        await db.SaveChangesAsync(ct);

        var roles = await userManager.GetRolesAsync(stored.User);
        return BuildResponse(stored.User, roles, now, newToken, newEntity.ExpiresAt);
    }

    /// <summary>Revokes a single refresh token (logout). Unknown tokens are ignored.</summary>
    public async Task RevokeAsync(string refreshToken, CancellationToken ct)
    {
        var hash = Hash(refreshToken);
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is { RevokedAt: null })
        {
            stored.RevokedAt = clock.GetUtcNow();
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken ct) =>
        await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);

    private AuthResponse BuildResponse(
        AppUser user, IList<string> roles, DateTimeOffset now, string refreshToken, DateTimeOffset refreshExpiresAt)
    {
        var accessExpiresAt = now.AddMinutes(_jwt.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(roles.Select(role => new Claim(AuthConstants.RoleClaim, role)));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = accessExpiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(SigningKey(_jwt), SecurityAlgorithms.HmacSha256),
        };

        var accessToken = new JsonWebTokenHandler().CreateToken(descriptor);

        return new AuthResponse(
            accessToken,
            accessExpiresAt,
            refreshToken,
            refreshExpiresAt,
            new UserDto(user.Id, user.Email ?? string.Empty, user.DisplayName, [.. roles.Order()]));
    }

    private (string Token, RefreshToken Entity) NewRefreshToken(Guid userId, DateTimeOffset now)
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        var entity = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            TokenHash = Hash(token),
            CreatedAt = now,
            ExpiresAt = now.AddDays(_jwt.RefreshTokenDays),
        };
        return (token, entity);
    }

    internal static SymmetricSecurityKey SigningKey(JwtOptions options) =>
        new(Encoding.UTF8.GetBytes(options.SigningKey));

    private static string Hash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
