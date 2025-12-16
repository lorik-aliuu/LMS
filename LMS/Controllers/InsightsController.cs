using LMS.Application.DTOs.AI;
using LMS.Application.DTOs.Insights;
using LMS.Application.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InsightsController : ControllerBase
    {
        private readonly ILibraryInsightsService _insightsService;

        public InsightsController(ILibraryInsightsService insightsService)
        {
            _insightsService = insightsService;
        }

      
        [HttpGet("library")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<LibraryInsightsDTO>> GetLibraryInsights()
        {
            try
            {
                var insights = await _insightsService.GenerateLibraryInsightsAsync(null);
                return Ok(insights);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating library insights", error = ex.Message });
            }
        }

      
        [HttpGet("my-insights")]
        [Authorize(Roles = "User")]
        public async Task<ActionResult<LibraryInsightsDTO>> GetMyInsights()
        {
            try
            {
                var userId = GetCurrentUserId();
                var insights = await _insightsService.GenerateLibraryInsightsAsync(userId);
                return Ok(insights);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating your insights", error = ex.Message });
            }
        }

       
        [HttpGet("my-habits")]
        [Authorize(Roles = "User")]
        public async Task<ActionResult<UserReadingHabitsDTO>> GetMyReadingHabits()
        {
            try
            {
                var userId = GetCurrentUserId();
                var habits = await _insightsService.SummarizeUserReadingHabitsAsync(userId);
                return Ok(habits);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error analyzing your habits", error = ex.Message });
            }
        }

        [HttpGet("user/{userId}/habits")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserReadingHabitsDTO>> GetUserReadingHabits(string userId)
        {
            try
            {
                var habits = await _insightsService.SummarizeUserReadingHabitsAsync(userId);
                return Ok(habits);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error analyzing user habits", error = ex.Message });
            }
        }

      
        private string GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("User ID not found  ");

            return userIdClaim; 
        }
    }
}
