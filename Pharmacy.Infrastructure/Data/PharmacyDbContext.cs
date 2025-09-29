using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Core.Models;

namespace Pharmacy.Infrastructure.Data
{
    public class PharmacyDbContext: IdentityDbContext<AppUser>
    {
        public PharmacyDbContext(DbContextOptions options):base(options)
        {
            
        }

        public DbSet<DeliveryMan> DeliveryMen { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
