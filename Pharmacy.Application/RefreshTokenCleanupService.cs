using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pharmacy.Infrastructure.Data;

namespace Pharmacy.Application
{
    public class RefreshTokenCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RefreshTokenCleanupService> _logger;

        public RefreshTokenCleanupService(
            IServiceProvider serviceProvider,
            ILogger<RefreshTokenCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogWarning("✅ RefreshTokenCleanup STARTED");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();

                    var now = DateTime.UtcNow;

                    var expiredTokens = await db.RefreshTokens
                        .Where(t => t.IsRevoked || t.ExpiresAt < now)
                        .ToListAsync(stoppingToken);

                    if (expiredTokens.Any())
                    {
                        db.RefreshTokens.RemoveRange(expiredTokens);
                        await db.SaveChangesAsync(stoppingToken);

                        _logger.LogWarning("🗑 Deleted {Count} expired/revoked tokens", expiredTokens.Count);
                    }
                    else
                    {
                        _logger.LogInformation("No expired/revoked tokens found at {Time}", now);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ ERROR in RefreshTokenCleanupService");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken); 
            }
        }
    }

}
