// File: RegExtention/AuthExtensions.cs
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Shortify.DTOs.AuthOptionDTOs;
using Shortify.Models.AuthOptions; // SignUpDtoValidator
using Shortify.Repositories;
using Shortify.Repositories.Interfaces;
using Shortify.Services;
using Shortify.Services.Interfaces;

namespace Shortify.RegExtention
{
    public static class AuthExtensions
    {
        /// <summary>
        /// Registers authentication, authorization and a minimal set of auth-related services.
        /// This version avoids registering types that may not exist in your project (TokenService,
        /// UserClaimsFactory, ApiKeyService, etc.) and does not attempt to register static helpers.
        /// Use IHostEnvironment to choose in-memory vs EF repository for development vs production.
        /// </summary>
        public static IServiceCollection AddShortifyAuth(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<AuthOptions>(config.GetSection("Auth"));
            var opts = config.GetSection("Auth").Get<AuthOptions>() ?? new AuthOptions();

            //env ??= new HostEnvironmentWrapper(); // or simply use EnvironmentName checks below

            // use env.IsDevelopment() safely (env may be null in some test scenarios)
            IHostEnvironment? env = null;
            var isDev = false && (env?.IsDevelopment() ?? string.Equals(
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                Environments.Development, StringComparison.OrdinalIgnoreCase));

            // Repository registration: use InMemory in Development, otherwise EF implementation.
            if (isDev)
            {
                services.AddScoped<IAuthRepository, InMemoryAuthRepository>();
            }
            else
            {
                services.AddScoped<IAuthRepository, AuthRepository>();
            }

            // Core auth service (concrete). Register the concrete type so callers can still resolve it.
            // If you have an IAuthService interface, replace the registration with services.AddScoped<IAuthService, AuthService>();
            //services.AddScoped<AuthService>();
            services.AddScoped<IAuthService, AuthService>();

            // Resolve-related registrations requested earlier
            services.AddScoped<IResolveEventRepository, ResolveEventRepository>();
            services.AddScoped<IResolveService, ResolveService>();

            // Register only validators/types that exist in the project.
            // SignUpDtoValidator exists in your codebase; SignInDtoValidator was missing so we don't register it here.
            services.AddTransient<IValidator<SignUpDto>, SignUpDtoValidator>();

            // Do not register static helpers (e.g., Pbkdf2PasswordCrypto) as services.
            // If you have an interface-based crypto/token service, register it here (example commented):
            // services.AddScoped<IPasswordCrypto, Pbkdf2PasswordCryptoImpl>();

            // Configure JWT authentication only if not explicitly disabled in configuration
            if (!opts.DisableAuth)
            {
                //services.AddAuthentication(options =>
                //{
                //    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                //    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                //})
                //.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, cfg =>
                //{
                //    cfg.RequireHttpsMetadata = false;
                //    cfg.TokenValidationParameters = new TokenValidationParameters
                //    {
                //        ValidateIssuer = true,
                //        ValidIssuer = opts.Jwt.Issuer,
                //        ValidateAudience = true,
                //        ValidAudience = opts.Jwt.Audience,
                //        ValidateIssuerSigningKey = true,
                //        IssuerSigningKey = new SymmetricSecurityKey(Convert.FromHexString(opts.Jwt.SigningKey)/*Encoding.UTF8.GetBytes(opts.Jwt.SigningKey)*/),
                //        ValidateLifetime = true
                //    };
                //});

                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = opts.Jwt.Issuer,
                        ValidAudience = opts.Jwt.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Convert.FromHexString(opts.Jwt.SigningKey)),//Convert.FromBase64String(configuration["Jwt:Key"])),
                        ClockSkew = TimeSpan.FromSeconds(60)
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = ctx =>
                        {
                            if (string.IsNullOrEmpty(ctx.Token) &&
                                ctx.Request.Cookies.TryGetValue("AccessToken", out var cookieToken) &&
                                !string.IsNullOrWhiteSpace(cookieToken))
                            {
                                ctx.Token = cookieToken;
                            }
                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = ctx =>
                        {
                            var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtAuth");
                            logger.LogWarning("JWT auth failed: {Message}", ctx.Exception?.Message);
                            return Task.CompletedTask;
                        }
                    };
                });
            }

            // Authorization: allow toggles for DisableAuth and PermissiveMode
            //services.AddAuthorization(options =>
            //{
            //    if (opts.DisableAuth)
            //    {
            //        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            //            .RequireAssertion(_ => true)
            //            .Build();
            //    }
            //    else if (opts.PermissiveMode)
            //    {
            //        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            //            .RequireAssertion(_ => true)
            //            .Build();
            //    }
            //});

            services.AddAuthorization();

            return services;
        }
    }
}
