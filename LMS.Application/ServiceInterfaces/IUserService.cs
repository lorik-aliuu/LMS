using LMS.Application.DTOs.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Application.ServiceInterfaces
{
    public interface IUserService
    {
        Task<UserProfileDTO> GetUserProfileAsync(string userId);
        Task<UserProfileDTO> UpdateUserProfileAsync(string userId, UpdateUserDTO updateDto);
        Task<bool> ChangePasswordAsync(string userId, ChangePasswordDTO changePasswordDto);
        Task<bool> DeleteAccountAsync(string userId, string password);

        Task<IEnumerable<UserProfileDTO>> GetAllUsersAsync();
        Task<UserProfileDTO> GetUserByIdAsync(string userId);
        Task<bool> DeleteUserAsync(string userId);
        Task<bool> UpdateUserRoleAsync(string userId, string role);
    }
}
