using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seatline.Api.Auth;

namespace Seatline.Api.Controllers;

public sealed record AreaResponse(string Area, string Message, string User);

/// <summary>
/// Placeholder endpoints that prove role-based access control end to end.
/// They will be replaced by the box office panel (block 5) and door validation (block 5).
/// </summary>
[ApiController]
[Produces("application/json")]
public sealed class RoleCheckController : ControllerBase
{
    /// <summary>Box office area: admins only.</summary>
    [HttpGet("api/admin/overview")]
    [Authorize(Policy = AuthPolicies.Admin)]
    [ProducesResponseType<AreaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<AreaResponse> AdminOverview() =>
        new AreaResponse("admin", "Welcome to the box office panel.", User.Identity?.Name ?? "unknown");

    /// <summary>Door area: validators (and admins) only.</summary>
    [HttpGet("api/door/status")]
    [Authorize(Policy = AuthPolicies.Door)]
    [ProducesResponseType<AreaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<AreaResponse> DoorStatus() =>
        new AreaResponse("door", "Ready to scan tickets.", User.Identity?.Name ?? "unknown");
}
