using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AreebTechnologyTask.Data;
using AreebTechnologyTask.Dto;
using AreebTechnologyTask.Enums;
using AreebTechnologyTask.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;

namespace AreebTechnologyTask.Controllers
{
    [Authorize]
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly TimeSpan _tokenLifetime = TimeSpan.FromHours(1); // JWT token lifetime

        // Used when the email does not exist, so a failed login takes the same time either way
        // (otherwise the response time would reveal which emails are registered).
        private static readonly string DummyPasswordHash = BCrypt.Net.BCrypt.HashPassword("not-a-real-password");


        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Already logged in? Go to the right start page instead of showing Login/Register again.
        private IActionResult? RedirectIfLoggedIn()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return null;
            }
            return User.IsInRole(nameof(UserRole.Admin))
                ? RedirectToAction("AdminPanel", "Admin")
                : RedirectToAction("Index", "Home");
        }

        // GET: /Auth/Login
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return RedirectIfLoggedIn() ?? View();
        }

        // POST: /Auth/Login
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto request, string? returnUrl)
        {
            var email = request.Email?.Trim() ?? string.Empty;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            // One message for "unknown email" and "wrong password", so attackers cannot find out
            // which emails are registered.
            var passwordOk = BCrypt.Net.BCrypt.Verify(request.Password ?? string.Empty, user?.HashedPassword ?? DummyPasswordHash);
            if (user == null || !passwordOk)
            {
                TempData["LoginError"] = "Invalid email or password.";
                return View(request);
            }

            string token = CreateToken(user, out DateTime expiresAt);

            Response.Cookies.Append("JwtToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt
            });

            // Redirect based on user role
            if (user.Role == UserRole.Admin)
            {
                return RedirectToAction("AdminPanel", "Admin");
            }
            else
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }
        }


        // Logout works even if the login already expired.
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            Response.Cookies.Delete("JwtToken"); // Remove JWT cookie

            HttpContext.Session.Clear();

            await Task.CompletedTask;

            return RedirectToAction("Index", "Home");
        }

        private string CreateToken(User user, out DateTime expiresAt)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            expiresAt = DateTime.UtcNow.Add(_tokenLifetime);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }



        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            return RedirectIfLoggedIn() ?? View();
        }

        // GET: /Auth/AccessDenied - shown when a logged-in user opens a page their role cannot use.
        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View("_Unauthorized");
        }

        // POST: /Auth/Register
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto request)
        {
            // Validate phone number format
            if (!string.IsNullOrEmpty(request.Phone))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(request.Phone, @"^01[0-9]{9}$"))
                {
                    ModelState.AddModelError("Phone", "Phone number must start with '01' and be exactly 11 digits");
                    return View(request);
                }
            }

            // One account per email address (the email is what users log in with).
            var email = request.Email?.Trim() ?? string.Empty;
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError("", "An account with this email already exists.");
                return View(request);
            }

            // Hash the password
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Name = request.Name,
                Email = email,
                HashedPassword = passwordHash,
                Address = request.Address,
                Phone = request.Phone,
                Role = UserRole.User
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }
    }
}
