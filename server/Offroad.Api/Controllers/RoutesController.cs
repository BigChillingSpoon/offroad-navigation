using Microsoft.AspNetCore.Mvc;
using Offroad.Api.Extensions;
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
            return result.ToActionResult(Ok);
        }

        //todo in future, list all by user, for now all routes are returned
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _routes.GetAllAsync(HttpContext.RequestAborted);
            return result.ToActionResult(ok => Ok(ok));
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] SaveRouteRequest request)
        {
            var result = await _routes.SaveAsync(request, HttpContext.RequestAborted);
            return result.ToActionResult(id => CreatedAtAction(nameof(GetById), new { id }, id));
        }

        // todo add soft delete
        [HttpDelete]
        public async Task<IActionResult> Delete([FromBody] DeleteRequest request)
        {
            var result = await _routes.DeleteAsync(request.Id, HttpContext.RequestAborted);
            return result.ToActionResult(_ => NoContent());
        }

        // todo add ability to plan route for specific user
        [HttpPost("plan")]
        public async Task<IActionResult> Plan([FromBody] PlanRouteRequest request)
        {
            var result = await _routes.PlanAsync(request, HttpContext.RequestAborted);
            return result.ToActionResult(Ok);
        }
    }
}
