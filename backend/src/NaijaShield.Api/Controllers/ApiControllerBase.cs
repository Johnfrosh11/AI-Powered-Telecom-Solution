using Microsoft.AspNetCore.Mvc;
using NaijaShield.Application.Common;

namespace NaijaShield.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleResult(Result result) =>
        result.IsSuccess ? NoContent() : BadRequest(new { errors = result.Error });

    protected IActionResult HandleResult<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Error });

    protected IActionResult HandleNotFound<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : NotFound(new { errors = result.Error });
}
