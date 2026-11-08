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

            // JWT signing key. It is a secret, so it is NOT in appsettings.json:
            //   your PC:   dotnet user-secrets set "Jwt:Key" "<long random text>"
            //   Docker:    JWT_KEY in the .env file (passed as the Jwt__Key environment variable)
            var jwtKey = builder.Configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
            {
                if (!builder.Environment.IsDevelopment())
                {
                    throw new InvalidOperationException(
                        "Jwt:Key is missing or shorter than 32 characters. Set it with user-secrets or the Jwt__Key environment variable.");
                }
                // Development only: use a temporary random key so a fresh clone runs without setup.
                // Logins are lost when the app restarts.
                jwtKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
                builder.Configuration["Jwt:Key"] = jwtKey;
                Console.WriteLine("warn: Jwt:Key is not configured; using a temporary random key (logins end when the app restarts).");
            }

            // Authentication: the JWT is stored in the "JwtToken" cookie and validated on every request,
            // so User.Identity.IsAuthenticated, User.IsInRole(...) and [Authorize] work everywhere.
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                    };

                    static bool IsAjax(HttpRequest request) =>
                        request.Headers.XRequestedWith == "XMLHttpRequest";

                    options.Events = new JwtBearerEvents
                    {
                        // Read the JWT from the cookie instead of the Authorization header.
                        OnMessageReceived = context =>
                        {
                            var token = context.Request.Cookies["JwtToken"];
                            if (!string.IsNullOrEmpty(token))
                            {
                                context.Token = token;
                            }
                            return Task.CompletedTask;
                        },

                        // Expired or invalid token: remove the cookie so the site shows the user as logged out.
                        OnAuthenticationFailed = context =>
                        {
                            context.Response.Cookies.Delete("JwtToken");
                            return Task.CompletedTask;
                        },

                        // Not logged in on a protected page: go to the login page (or 401 for AJAX calls).
                        OnChallenge = context =>
                        {
                            context.HandleResponse();
                            if (IsAjax(context.Request))
                            {
                                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                return context.Response.WriteAsJsonAsync(new { success = false, message = "Please log in first." });
                            }
                            var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
                            context.Response.Redirect("/Auth/Login?returnUrl=" + Uri.EscapeDataString(returnUrl));
                            return Task.CompletedTask;
                        },

                        // Logged in but wrong role (e.g. a user opening the admin panel).
                        OnForbidden = context =>
                        {
                            if (IsAjax(context.Request))
                            {
                                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                                return context.Response.WriteAsJsonAsync(new { success = false, message = "You do not have permission to do that." });
                            }
                            context.Response.Redirect("/Auth/AccessDenied");
                            return Task.CompletedTask;
                        }
                    };
                });

            // Authorization: controllers use [Authorize(Roles = nameof(UserRole.Admin))] etc.
            builder.Services.AddAuthorization();

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
