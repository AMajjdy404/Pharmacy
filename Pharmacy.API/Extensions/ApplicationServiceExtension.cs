using Pharmacy.API.Helpers;
using Pharmacy.Application;
using Pharmacy.Core.Interfaces;
using Pharmacy.Infrastructure.Implementation;

namespace Pharmacy.API.Extensions
{
    public static class ApplicationServiceExtension
    {
        public static IServiceCollection AddApplicationService(this IServiceCollection Services)
        {
            Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            //Services.AddScoped<INotificationService, NotificationService>();
            //Services.AddAutoMapper(typeof(MappingProfiles));
            Services.AddScoped<DataSeeder>();
            Services.AddHostedService<RefreshTokenCleanupService>();
            return Services;
        }
    }
}
