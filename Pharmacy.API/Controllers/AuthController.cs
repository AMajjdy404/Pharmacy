using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.API.Dtos;
using Pharmacy.API.Errors;
using Pharmacy.Core.Interfaces;
using Pharmacy.Core.Models;
using Pharmacy.Core.Services;

namespace Pharmacy.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IPasswordHasher<DeliveryMan> _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly IGenericRepository<DeliveryMan> _deliveryManRepo;
        private readonly IGenericRepository<RefreshToken> _refreshTokenRepo;

        public AuthController(
            UserManager<AppUser> userManager,
            IPasswordHasher<DeliveryMan> passwordHasher,
            ITokenService tokenService,
            IGenericRepository<DeliveryMan> deliveryManRepo,
            IGenericRepository<RefreshToken> refreshTokenRepo)
        {
            _userManager = userManager;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _deliveryManRepo = deliveryManRepo;
            _refreshTokenRepo = refreshTokenRepo;
        }




        // ✅ Login (AppUser or DeliveryMan)
        [HttpPost("login-appuser")]
        public async Task<IActionResult> LoginAppUser(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
                return Unauthorized(new ApiResponse(401, "Invalid email or password"));

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var (accessToken, refreshToken) = await _tokenService.CreateTokenAsync(user, _userManager, ip, dto.RememberMe);

            // Save Refresh Token
            refreshToken.OwnerId = user.Id;
            refreshToken.UserType = "AppUser";
            await _refreshTokenRepo.AddAsync(refreshToken);
            await _refreshTokenRepo.SaveChangesAsync();

            return Ok(new { accessToken, refreshToken = refreshToken.Token, refreshToken.ExpiresAt });
        }

        [HttpPost("login-deliveryman")]
        public async Task<IActionResult> LoginDeliveryMan(LoginDto dto)
        {
            var deliveryMan = await _deliveryManRepo.GetFirstOrDefaultAsync(d => d.Email == dto.Email);
            if (deliveryMan == null)
                return Unauthorized(new ApiResponse(401, "Invalid email or password"));

            var result = _passwordHasher.VerifyHashedPassword(deliveryMan, deliveryMan.Password, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                return Unauthorized(new ApiResponse(401, "Invalid email or password"));

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var (accessToken, refreshToken) = await _tokenService.CreateTokenAsync(deliveryMan, ip, dto.RememberMe);

            refreshToken.OwnerId = deliveryMan.Id.ToString();
            refreshToken.UserType = "DeliveryMan";
            await _refreshTokenRepo.AddAsync(refreshToken);
            await _refreshTokenRepo.SaveChangesAsync();

            return Ok(new { accessToken, refreshToken.Token, refreshToken.ExpiresAt });
        }

        // ✅ Refresh
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto dto)
        {
            var oldToken = await _refreshTokenRepo.GetFirstOrDefaultAsync(r => r.Token == dto.RefreshToken);
            if (oldToken == null || oldToken.IsRevoked || oldToken.ExpiresAt < DateTime.UtcNow)
                return Unauthorized(new ApiResponse(401, "Invalid refresh token"));

            if (oldToken.ReplacedByToken != null)
                return Unauthorized(new ApiResponse(401, "Refresh token already used"));

            // Mark old token revoked
            oldToken.IsRevoked = true;
            oldToken.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepo.SaveChangesAsync();

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            (string accessToken, RefreshToken newRefreshToken) tokenResult;

            if (oldToken.UserType == "AppUser")
            {
                var user = await _userManager.FindByIdAsync(oldToken.OwnerId);
                if (user == null) return Unauthorized(new ApiResponse(401, "User not found"));

                tokenResult = await _tokenService.CreateTokenAsync(user, _userManager, ip);
            }
            else
            {
                var deliveryMan = await _deliveryManRepo.GetByIdAsync(int.Parse(oldToken.OwnerId));
                if (deliveryMan == null) return Unauthorized(new ApiResponse(401, "User not found"));

                tokenResult = await _tokenService.CreateTokenAsync(deliveryMan, ip);
            }

            // Save new refresh token
            tokenResult.newRefreshToken.OwnerId = oldToken.OwnerId;
            tokenResult.newRefreshToken.UserType = oldToken.UserType;
            oldToken.ReplacedByToken = tokenResult.newRefreshToken.Token;

            await _refreshTokenRepo.AddAsync(tokenResult.newRefreshToken);
            await _refreshTokenRepo.SaveChangesAsync();

            return Ok(new { accessToken = tokenResult.accessToken, refreshToken = tokenResult.newRefreshToken.Token });
        }

        // ✅ Revoke
        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke([FromBody] RefreshRequestDto dto)
        {
            var token = await _refreshTokenRepo.GetFirstOrDefaultAsync(r => r.Token == dto.RefreshToken);
            if (token == null) return NotFound(new ApiResponse(404, "Token not found"));

            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepo.SaveChangesAsync();

            return Ok("Token revoked successfully");
        }
    }

}
