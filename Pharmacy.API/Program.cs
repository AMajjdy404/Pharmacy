
using Microsoft.EntityFrameworkCore;
using Pharmacy.API.Extensions;
using Pharmacy.API.Helpers;
using Pharmacy.API.Hubs;
using Pharmacy.API.Middlewares;
using Pharmacy.Infrastructure.Data;
using Serilog;
using Serilog.Formatting.Json;

namespace Pharmacy.API
{
    public class Program
    {
        public async static Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Services.AddSignalR();


            builder.Host.UseSerilog((context, configuration) =>
            {
                configuration
                    .WriteTo.File(
                        path: Path.Combine(context.HostingEnvironment.ContentRootPath, "logs", "error.log"),
                        formatter: new JsonFormatter(),
                        rollingInterval: RollingInterval.Day)
                    .Enrich.FromLogContext()
                    .MinimumLevel.Error();
            });

            builder.Services.AddDbContext<PharmacyDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // Extension Methods
            builder.Services.AddIdentityService(builder.Configuration);
            builder.Services.AddApplicationService();

            var app = builder.Build();

            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();

            try
            {
                var dbContext = services.GetRequiredService<PharmacyDbContext>();
                var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
                await dbContext.Database.MigrateAsync(); // Update Database
                await seeder.SeedAsync(); // Account & Roles Seeding
            }
            catch (Exception ex)
            {
                var logger = loggerFactory.CreateLogger<Program>();
                logger.LogError(ex, "This Error Happened During Applying Migration");
            }

            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapHub<NotificationHub>("/hubs/notification");
            app.UseMiddleware<ExceptionMiddleware>();
            app.UseDeveloperExceptionPage();
            app.MapControllers();
            app.Run();
        }
    }
}
