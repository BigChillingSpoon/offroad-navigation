using Microsoft.AspNetCore.Mvc;
using Offroad.Core;
using Routing.Application.Contracts;
using Routing.Application.Contracts.Models;

namespace Offroad.Api.Controllers
{
    [ApiController]
    [Route("api/loops")]
    public sealed class LoopsController : ControllerBase
    {
        private readonly ILoopsModule _loopsModule;

        public LoopsController(ILoopsModule loopsModule)
        {
            _loopsModule = loopsModule;
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _loopsModule.GetByIdAsync(id, HttpContext.RequestAborted);

            return result.Match(
                ok => Ok(ok),
                err => err.Type switch
                {
                    ErrorType.NotFound => NotFound(err.Message),
                    _ => StatusCode(500, err.Message)
                }
            );
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _loopsModule.GetAllAsync(HttpContext.RequestAborted);

            return result.Match(
                ok => Ok(ok),
                err => StatusCode(500, err.Message)
            );
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] SaveLoopRequest request)
        {
            var result = await _loopsModule.SaveAsync(request, HttpContext.RequestAborted);

            return result.Match(
                ok => CreatedAtAction(nameof(GetById), new { id = ok }, ok),
                err => err.Type switch
                {
                    ErrorType.Validation => BadRequest(err.Message),
                    _ => StatusCode(500, err.Message)
                }
            );
        }

        [HttpDelete]
        public async Task<IActionResult> Delete([FromBody] DeleteRequest request)
        {
            var result = await _loopsModule.DeleteAsync(request.Id, HttpContext.RequestAborted);

            return result.Match<IActionResult>(
                ok => NoContent(),
                err => err.Type switch
                {
                    ErrorType.NotFound => NotFound(err.Message),
                    _ => StatusCode(500, err.Message)
                }
            );
        }

        [HttpPost("find")]
        public async Task<IActionResult> Find([FromBody] FindLoopsRequest request)
        {
            var result = await _loopsModule.FindAsync(request, HttpContext.RequestAborted);

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


