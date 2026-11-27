using System.ComponentModel.DataAnnotations;

namespace Seatline.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = "seatline-api";

    [Required]
    public string Audience { get; init; } = "seatline-web";

    /// <summary>HMAC-SHA256 key. At least 32 characters; set it through the environment, never in git.</summary>
    [Required]
    [MinLength(32, ErrorMessage = "Jwt:SigningKey must be at least 32 characters long.")]
    public string SigningKey { get; init; } = string.Empty;

    [Range(1, 120)]
    public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, 90)]
    public int RefreshTokenDays { get; init; } = 7;
}
