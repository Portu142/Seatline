using System.ComponentModel.DataAnnotations;

namespace Seatline.Api.Auth;

public static class AuthConstants
{
    /// <summary>Claim type used for roles inside our JWTs (short name instead of the long WS-* URI).</summary>
    public const string RoleClaim = "role";

    public const string AuthRateLimitPolicy = "auth";
}

public sealed record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, StringLength(128, MinimumLength = 8)] string Password,
    [Required, StringLength(80, MinimumLength = 2)] string DisplayName);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record RefreshRequest([Required] string RefreshToken);

public sealed record UserDto(Guid Id, string Email, string DisplayName, IReadOnlyList<string> Roles);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserDto User);
