using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Pharmacy.Core.Models;

namespace Pharmacy.Core.Services
{
    public interface ITokenService
    {
        Task<(string accessToken, RefreshToken refreshToken)> CreateTokenAsync(
            AppUser appUser,
            UserManager<AppUser> userManager,
            string ipAddress,
            bool rememberMe = false);

        Task<(string accessToken, RefreshToken refreshToken)> CreateTokenAsync(
            DeliveryMan deliveryMan,
            string ipAddress,
            bool rememberMe = false);
    }

}
