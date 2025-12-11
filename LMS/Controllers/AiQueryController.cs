using LMS.Application.DTOs.AI;
using LMS.Application.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AiQueryController : ControllerBase
    {
        private readonly IAiQueryService _aiQueryService;

        public AiQueryController(IAiQueryService aiQueryService)
        {
            _aiQueryService = aiQueryService;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User ID not found ");
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }

        [HttpPost("query")]
        public async Task<IActionResult> ProcessQuery([FromBody] AiQueryRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Query cannot be empty"
                });
            }

            var userId = GetCurrentUserId();
            var isAdmin = IsAdmin();

            var result = await _aiQueryService.ProcessQueryAsync(request.Query, userId, isAdmin);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpGet("examples")]
        public IActionResult GetExampleQueries()
        {
            var isAdmin = IsAdmin();

            var userExamples = new List<string>
        {
            "Show my most expensive books",
            "How many books do I have?",
            "Show me my statistics",
            "What books am I currently reading?",
            "Show me my programming books",
            "What's my most common genre?"
        };

            var adminExamples = new List<string>
        {
            "Who owns the most books?",
            "Which is the most popular book?",
            "Show the five most expensive books",
            "What are the general library statistics?",
            "Show books by genre Fantasy",
            "Show the completed books"
        };

            return Ok(new
            {
                success = true,
                data = isAdmin ? adminExamples : userExamples
            });
        }
    }
}
