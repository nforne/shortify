using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shortify.Repositories;
using Shortify.Repositories.Interfaces;
using Shortify.Services;
using Shortify.Services.Interfaces;

namespace Shortify.RegExtention
{
    public static class ServiceRegistrationExtensions
    {
        public static IServiceCollection AddShortifyUsers(this IServiceCollection services, IConfiguration config)
        {
            services.AddHttpContextAccessor();

            // IMPORTANT: ShortifyDbContext must be registered in Program.cs (AddDbContext<ShortifyDbContext>(...))
            // Register repositories first
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAttributeRepository, AttributeRepository>();
            services.AddScoped<IGroupRepository, GroupRepository>();
            services.AddScoped<IMetricRepository, MetricRepository>();

            // Register auth context
            services.AddScoped<IAuthContext, HeaderAuthContext>();

            // Register snapshot storage (dev default). Replace with S3SnapshotStorage when ready.
            var snapshotsFolder = config.GetValue<string>("Snapshots:LocalFolder") ?? "./snapshots";
            var snapshotsBaseUrl = config.GetValue<string>("Snapshots:BaseUrl") ?? "";       
            services.AddSingleton<ISnapshotStorage>(sp => new MetricSnapshotStorage(snapshotsFolder, snapshotsBaseUrl));

            // Register services (let DI construct MetricService including ShortifyDbContext)
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAttributeService, AttributeService>();
            services.AddScoped<IGroupService, GroupService>();

            // Ensure MetricService is registered by its implementation so DI can resolve ShortifyDbContext automatically
            services.AddScoped<IMetricService, MetricService>();

            return services;
        }
    }
}
