using Microsoft.AspNetCore.Mvc;

namespace SaaS.Api.Controllers;

[ApiController]
[Route("healthz")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new { status = "healthy" });
}
