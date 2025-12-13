using LMS.Application.DTOs.Recommendation;
using LMS.Application.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RecommendationController : ControllerBase
    {
        private readonly IRecommendationService _recommendationService;

        public RecommendationController(IRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User ID not found");
        }

        [HttpPost]
        public async Task<IActionResult> GetRecommendations([FromBody] RecommendationRequestDTO request)
        {
            var userId = GetCurrentUserId();
            var result = await _recommendationService.GetRecommendationsAsync(userId, request);

            return Ok(result);
        }

        [HttpPost("save")]
        public async Task<IActionResult> SaveRecommendedBook([FromBody] SaveRecommendationRequestDTO request)
        {
            var userId = GetCurrentUserId();
            var result = await _recommendationService.SaveRecommendedBookAsync(userId, request);
            return Ok(result);
        }

        [HttpPost("dismiss")]
        public async Task<IActionResult> DismissRecommendedBook([FromBody] DismissRecommendationDTO request)
        {
            var userId = GetCurrentUserId();
            var result = await _recommendationService.DismissRecommendedBookAsync(userId, request);
            return Ok(result);
        }
    }



}
