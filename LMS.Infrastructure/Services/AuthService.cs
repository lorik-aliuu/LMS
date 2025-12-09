using LMS.Application.DTOs.Auth;
using LMS.Application.ServiceInterfaces;
using LMS.Domain.Exceptions;
using LMS.Infrastructure.Identity;
using LMS.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IJwtTokenService jwtTokenService,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _context = context;
            _configuration = configuration;
        }


        public async Task<AuthResponseDTO?> RegisterAsync(RegisterDTO registerDto)
        {

            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                throw new ValidationException("User with this email already exists");
            }


            var user = new ApplicationUser
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                UserName = registerDto.UserName,
                Email = registerDto.Email,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };  

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ValidationException($"Registration failed: {errors}");
            }


            await _userManager.AddToRoleAsync(user, "User");


            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _jwtTokenService.GenerateAccessToken(user, roles);
            var jwtId = GetJwtIdFromToken(accessToken);
            var refreshToken = _jwtTokenService.GenerateRefreshToken(user.Id, jwtId);


            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            return new AuthResponseDTO
            {
                Token = accessToken,
                RefreshToken = refreshToken.Token,
                UserId = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Role = roles.FirstOrDefault() ?? "User",
                ExpiresAt = DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"]!))
            };
        }

        public async Task<AuthResponseDTO> LoginAsync(LoginDTO loginDto)
        {

            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                throw new UnauthorizedException("Invalid email or password");
            }


            var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!isPasswordValid)
            {
                throw new UnauthorizedException("Invalid email or password");
            }


            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _jwtTokenService.GenerateAccessToken(user, roles);
            var jwtId = GetJwtIdFromToken(accessToken);
            var refreshToken = _jwtTokenService.GenerateRefreshToken(user.Id, jwtId);


            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            return new AuthResponseDTO
            {
                Token = accessToken,
                RefreshToken = refreshToken.Token,
                UserId = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                FirstName = user.FirstName,  
                LastName = user.LastName,     
                FullName = user.FullName,
                Role = roles.FirstOrDefault() ?? "User",
                ExpiresAt = DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"]!))
            };
        }

        public async Task<AuthResponseDTO> RefreshTokenAsync(string accessToken, string refreshToken)
        {

            var principal = _jwtTokenService.GetPrincipalFromExpiredToken(accessToken);
            if (principal == null)
            {
                throw new UnauthorizedException("Invalid access token");
            }


            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var jwtId = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(jwtId))
            {
                throw new UnauthorizedException("Invalid token claims");
            }


            var storedRefreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (storedRefreshToken == null)
            {
                throw new NotFoundException("Refresh token not found");
            }


            if (storedRefreshToken.IsUsed)
            {
                throw new UnauthorizedException("Refresh token has already been used");
            }

            if (storedRefreshToken.IsRevoked)
            {
                throw new UnauthorizedException("Refresh token has been revoked");
            }

            if (storedRefreshToken.ExpiresAt < DateTime.UtcNow)
            {
                throw new UnauthorizedException("Refresh token has expired");
            }

            if (storedRefreshToken.JwtId != jwtId)
            {
                throw new UnauthorizedException("Refresh token does not match access token");
            }

            if (storedRefreshToken.UserId != userId)
            {
                throw new UnauthorizedException("Refresh token does not belong to this user");
            }


            storedRefreshToken.IsUsed = true;
            _context.RefreshTokens.Update(storedRefreshToken);


            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }


            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = _jwtTokenService.GenerateAccessToken(user, roles);
            var newJwtId = GetJwtIdFromToken(newAccessToken);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken(user.Id, newJwtId);


            await _context.RefreshTokens.AddAsync(newRefreshToken);
            await _context.SaveChangesAsync();

            return new AuthResponseDTO
            {
                Token = newAccessToken,
                 RefreshToken = newRefreshToken.Token,
                UserId = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                FirstName = user.FirstName,  
                LastName = user.LastName,     
                FullName = user.FullName,
                Role = roles.FirstOrDefault() ?? "User",
                ExpiresAt = DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"]!))
            };
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
        {
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (storedToken == null)
            {
                return false;
            }

            storedToken.IsRevoked = true;
            _context.RefreshTokens.Update(storedToken);
            await _context.SaveChangesAsync();

            return true;
        }

        private string GetJwtIdFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Id;
        }
    }
}
