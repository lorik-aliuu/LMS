using LMS.Application.DTOs.Users;
using LMS.Application.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        { 
            _userService = userService;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User ID not found");
        }



        [HttpGet("profile")]
       
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetCurrentUserId();
            var profile = await _userService.GetUserProfileAsync(userId);

            return Ok(new
            {
                success = true,
                data = profile
            });
        }

        [HttpPut("profile")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserDTO updateDto)
        {
            var userId = GetCurrentUserId();
            var profile = await _userService.UpdateUserProfileAsync(userId, updateDto);

            return Ok(new
            {
                success = true,
                message = "Profile updated",
                data = profile
            });
        }

        [HttpPost("change-password")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO changePasswordDto)
        {
            var userId = GetCurrentUserId();
            await _userService.ChangePasswordAsync(userId, changePasswordDto);

            return Ok(new
            {
                success = true,
                message = "Password changed successfully"
            });
        }

        [HttpDelete("account")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> DeleteMyAccount([FromBody] DeleteAccountDTO deleteDto)
        {
            var userId = GetCurrentUserId();
            await _userService.DeleteAccountAsync(userId, deleteDto.Password);

            return Ok(new
            {
                success = true,
                message = "Account deleted "
            });
        }

        //ADMIN
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();

            return Ok(new
            {
                success = true,
                data = users
            });
        }

        [HttpGet("{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserById(string userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);

            return Ok(new
            {
                success = true,
                data = user
            });
        }

        [HttpDelete("{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            await _userService.DeleteUserAsync(userId);

            return Ok(new
            {
                success = true,
                message = "User deleted"
            });
        }

        [HttpPut("{userId}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserRole(string userId, [FromBody] UpdateRoleDTO roleDto)
        {
            await _userService.UpdateUserRoleAsync(userId, roleDto.Role);

            return Ok(new
            {
                success = true,
                message = "User role updated "
            });
        }
    }
}
