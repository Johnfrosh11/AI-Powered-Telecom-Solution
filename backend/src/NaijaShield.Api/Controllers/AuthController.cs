using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Application.Features.Auth;

namespace NaijaShield.Api.Controllers;

public class AuthController(IMediator mediator, ICurrentUserService currentUser) : ApiControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Login([FromBody] LoginCommand cmd, CancellationToken ct) =>
        HandleResult(await mediator.Send(cmd, ct));

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RefreshResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand cmd, CancellationToken ct) =>
        HandleResult(await mediator.Send(cmd, ct));

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand cmd, CancellationToken ct) =>
        HandleResult(await mediator.Send(cmd, ct));

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), 200)]
    public async Task<IActionResult> Me(CancellationToken ct) =>
        HandleResult(await mediator.Send(new GetCurrentUserQuery(currentUser.UserId ?? Guid.Empty), ct));
}
