
using Shortify.Data;
using Shortify.Services;
using Microsoft.OpenApi;
using Shortify.RegExtention;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Shortify
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            //builder.Services.AddEndpointsApiExplorer();
            //builder.Services.AddSwaggerGen();

            // -- Configuration / Services -------------------------------------------------
            builder.Services.AddControllers();

            // Swagger (OpenAPI)
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(static c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Shortify - Users API", Version = "v1" });
                // API key header definition for testing
                c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Name = "x-api-key",
                    Type = SecuritySchemeType.ApiKey,
                    Description = "API key header"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" } },
                        Array.Empty<string>()
                    }
                });
            });


            // DB Context (expect connection string in appsettings.Development.json under ConnectionStrings:DefaultConnection)
            var conn = builder.Configuration.GetConnectionString("ShortifyConnection");
            builder.Services.AddDbContext<ShortifyDbContext>(opts => opts.UseSqlServer(conn));

            // Register Users pieces (repository, service, auth context, http context accessor)
            builder.Services.AddShortifyUsers(builder.Configuration);

            // Allow minimal CORS for local testing (adjust as needed)
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(p => p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
            });


            // using Microsoft.AspNetCore.Authentication.JwtBearer;
            // using Microsoft.IdentityModel.Tokens;
            // using System.Text;

            var key = builder.Configuration["Jwt:SigningKey"] ?? "dev-placeholder-key-change-me";
            var issuer = builder.Configuration["Jwt:Issuer"] ?? "shortify";
            var audience = builder.Configuration["Jwt:Audience"] ?? "shortify-audience";

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.RequireHttpsMetadata = false; // set true in prod
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ValidateLifetime = true
                };
            });

            //app.UseEndpoints(endpoints => { endpoints.MapControllers(); });


            var app = builder.Build();

            // -- Middleware pipeline -----------------------------------------------------
            app.UseExceptionMapping();


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                //app.UseSwagger();
                //app.UseSwaggerUI();

                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Shortify - Users API v1"));
            }
                        
            app.UseCors();
            app.UseRouting();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();


            // -- Ensure DB and Seed ------------------------------------------------------
            using (var scope = app.Services.CreateScope())
            {
                var svc = scope.ServiceProvider;
                var db = svc.GetRequiredService<ShortifyDbContext>();
                // Ensure DB exists (dev) and run seeder
                db.Database.EnsureCreated();
                //await DbSeeder.SeedAsync(db);
            }

            app.Run();
        }
    }
}
