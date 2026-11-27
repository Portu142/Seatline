namespace Seatline.Api.Auth;

/// <summary>Application roles. Names are lowercase because they travel inside JWTs and URLs.</summary>
public static class Roles
{
    public const string Customer = "customer";
    public const string Admin = "admin";
    public const string Validator = "validator";

    public static readonly IReadOnlyList<string> All = [Customer, Admin, Validator];
}
