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

            // DbContext registration is expected in Program.cs; keep this idempotent
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthContext, HeaderAuthContext>();

            // group service
            services.AddScoped<IGroupRepository, GroupRepository>();
            services.AddScoped<IGroupService, GroupService>();

            return services;
        }
    }
}
