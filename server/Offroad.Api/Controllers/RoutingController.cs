using Microsoft.AspNetCore.Mvc;
using Offroad.Core;
using Routing.Application.Contracts;

namespace Offroad.Api.Controllers;

[ApiController]
[Route("api/routes")]
public sealed class RoutesController : ControllerBase
{
    private readonly IRoutingModule _routing;

    public RoutesController(IRoutingModule routing)
    {
        _routing = routing;
    }

    [HttpGet("{routeId:guid}")]
    public async Task<IActionResult> Get(Guid routeId)
    {
        var result = await _routing.GetRouteAsync(routeId);

        return result.Match(
            ok => Ok(ok),
            err => err.Type switch
            {
                ErrorType.NotFound => NotFound(err.Message),
                ErrorType.Validation => BadRequest(err.Message),
                _ => StatusCode(500, err.Message)
            }
        );
    }
}
