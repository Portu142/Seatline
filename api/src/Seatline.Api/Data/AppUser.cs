using Microsoft.AspNetCore.Identity;

namespace Seatline.Api.Data;

public class AppUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; } = [];
}
