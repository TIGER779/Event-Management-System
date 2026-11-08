using System.Text;
using AreebTechnologyTask.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AreebTechnologyTask.Enums;

namespace AreebTechnologyTask
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Database. "ConnectionStrings:Default" comes from (later ones win):
            //   appsettings.json      -> LocalDB, works on any Windows machine with Visual Studio
            //   user-secrets          -> your own SQL Server (dotnet user-secrets set "ConnectionStrings:Default" "...")
            //   environment variables -> ConnectionStrings__Default, used by docker-compose.yml
            var connectionString = builder.Configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:Default' is not configured.");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString, sql =>
                    // Retries briefly if SQL Server is not reachable yet (e.g. its container is still starting).
                    sql.EnableRetryOnFailure(maxRetryCount: 6, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Add session support
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
            });

            // Add authentication and JWT bearer configuration
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                    };

                    // READ JWT FROM COOKIE
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var token = context.Request.Cookies["JwtToken"];
                            if (!string.IsNullOrEmpty(token))
                            {
                                context.Token = token;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            // Add authorization services with policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdminRole", policy =>
                    policy.RequireRole(UserRole.Admin.ToString()));
                
                options.AddPolicy("RequireUserRole", policy =>
                    policy.RequireRole(UserRole.User.ToString()));
            });

            var app = builder.Build();

            // Create/upgrade the database and add sample data to an empty one.
            await DbSeeder.MigrateAndSeedAsync(app.Services);

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // Files requested with a content-hash version (?v=..., added by asp-append-version)
            // never change at that URL, so browsers may cache them for a year.
            app.Use(async (context, next) =>
            {
                if (context.Request.Query.ContainsKey("v") && Path.HasExtension(context.Request.Path.Value))
                {
                    context.Response.OnStarting(() =>
                    {
                        if (context.Response.StatusCode == StatusCodes.Status200OK ||
                            context.Response.StatusCode == StatusCodes.Status304NotModified)
                        {
                            context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
                        }
                        return Task.CompletedTask;
                    });
                }
                await next();
            });

            app.UseSession();

            app.UseRouting();

            // Fallback for files added to wwwroot after the build (e.g. new event images).
            // Placed after UseRouting, it only serves files that MapStaticAssets() has no endpoint for,
            // so build-time assets still get MapStaticAssets' compression and ETags.
            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseAuthorization();

            // Map the controller routes
            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            await app.RunAsync();
        }
    }
}
