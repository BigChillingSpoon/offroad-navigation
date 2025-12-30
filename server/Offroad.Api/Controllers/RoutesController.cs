using Microsoft.AspNetCore.Mvc;
using Offroad.Core;
using Routing.Application.Contracts;
using Routing.Application.Contracts.Models;

namespace Offroad.Api.Controllers
{
    [ApiController]
    [Route("api/routes")]
    public sealed class RoutesController : ControllerBase
    {
        private readonly IRoutesModule _routes;

        public RoutesController(IRoutesModule routes)
        {
            _routes = routes;
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _routes.GetByIdAsync(id, HttpContext.RequestAborted);

            return result.Match(
                ok => Ok(ok),
                err => err.Type switch
                {
                    ErrorType.NotFound => NotFound(err.Message),
                    _ => StatusCode(500, err.Message)
                }
            );
        }

        //todo in future, list all by user, for now all routes are returned
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _routes.GetAllAsync(HttpContext.RequestAborted);

            return result.Match(
                ok => Ok(ok),
                err => StatusCode(500, err.Message)
            );
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] SaveRouteRequest request)
        {
            var result = await _routes.SaveAsync(request, HttpContext.RequestAborted);

            return result.Match(
                ok => CreatedAtAction(nameof(GetById), new { id = ok }, ok),
                err => err.Type switch
                {
                    ErrorType.Validation => BadRequest(err.Message),
                    _ => StatusCode(500, err.Message)
                }
            );
        }
        // todo add soft delete
        [HttpDelete]
        public async Task<IActionResult> Delete([FromBody] DeleteRequest request)
        {
            var result = await _routes.DeleteAsync(request.Id, HttpContext.RequestAborted);

            return result.Match<IActionResult>(
                ok => NoContent(),
                err => err.Type switch
                {
                    ErrorType.NotFound => NotFound(err.Message),
                    _ => StatusCode(500, err.Message)
                }
            );
        }
        // todo add ability to plan route for specific user
        [HttpPost("plan")]
        public async Task<IActionResult> Plan([FromBody] PlanRouteRequest request)
        {
            var result = await _routes.PlanAsync(request, HttpContext.RequestAborted);

            return result.Match(
                ok => Ok(ok),
                err => err.Type switch
                {
                    ErrorType.Validation => BadRequest(err.Message),
                    _ => StatusCode(500, err.Message)
                }
            );
        }
    }
}


