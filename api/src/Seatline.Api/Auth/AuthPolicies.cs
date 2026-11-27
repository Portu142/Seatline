namespace Seatline.Api.Auth;

public static class AuthPolicies
{
    /// <summary>Box office: create shows, see sales.</summary>
    public const string Admin = "admin";

    /// <summary>Door: validate tickets. Admins can also do it.</summary>
    public const string Door = "door";
}
