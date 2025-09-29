using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Pharmacy.Core.Models;
using Pharmacy.Core.Services;

namespace Pharmacy.Application
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<(string accessToken, RefreshToken refreshToken)> CreateTokenAsync(
            AppUser appUser,
            UserManager<AppUser> userManager,
            string ipAddress,
            bool rememberMe = false)
        {
            var authClaims = new List<Claim>
{
             new Claim(ClaimTypes.NameIdentifier, appUser.Id.ToString()),
             new Claim(ClaimTypes.Email, appUser.Email),
             new Claim(ClaimTypes.Name, appUser.UserName),
             new Claim("UserType", "AppUser")
};

            var userRoles = await userManager.GetRolesAsync(appUser);
            Console.WriteLine($"TokenService - Roles for {appUser.Email}: {string.Join(",", userRoles)}");

            foreach (var role in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            Console.WriteLine("Claims before generating token:");
            foreach (var claim in authClaims)
            {
                Console.WriteLine($"{claim.Type} = {claim.Value}");
            }


            var accessToken = GenerateJwtToken(authClaims);

            var refreshToken = GenerateRefreshToken(
                ipAddress,
                appUser.Id,
                "AppUser",
                rememberMe
            );

            return (accessToken, refreshToken);
        }

        public async Task<(string accessToken, RefreshToken refreshToken)> CreateTokenAsync(
            DeliveryMan deliveryMan,
            string ipAddress,
            bool rememberMe = false)
        {
            var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, deliveryMan.Id.ToString()),
            new Claim(ClaimTypes.Email, deliveryMan.Email),
            new Claim("UserType", "DeliveryMan")
        };

            var accessToken = GenerateJwtToken(authClaims);

            var refreshToken = GenerateRefreshToken(
                ipAddress,
                deliveryMan.Id.ToString(),
                "DeliveryMan",
                rememberMe
            );

            return (accessToken, refreshToken);
        }

        private string GenerateJwtToken(List<Claim> authClaims)
        {
            var authKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Key"]));

            var expirationMinutes = double.Parse(_configuration["JWT:AccessTokenDurationInMinutes"]);

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public RefreshToken GenerateRefreshToken(string ipAddress, string ownerId, string userType, bool rememberMe = false)
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            var durationDays = rememberMe
                ? double.Parse(_configuration["JWT:RefreshTokenRememberMeDurationInDays"])
                : double.Parse(_configuration["JWT:RefreshTokenDurationInDays"]);

            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomBytes),
                OwnerId = ownerId,
                UserType = userType,
                ExpiresAt = DateTime.UtcNow.AddDays(durationDays),
                IsRevoked = false
            };
        }

       
    }



}
