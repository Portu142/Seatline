namespace Seatline.Api.Data;

/// <summary>
/// A long-lived token used to obtain new access tokens. Only a SHA-256 hash is stored,
/// so a database leak does not leak usable tokens. Tokens are single-use: every refresh
/// revokes the presented token and issues a new one (rotation).
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public AppUser User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;
}
