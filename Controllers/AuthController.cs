using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using transactioninquiry.Classes.Utils;
using transactioninquiry.Data;
using transactioninquiry.Models;

namespace transactioninquiry.Controllers;

public class AuthController : Controller
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly JwtService _jwtService;

    public AuthController(AppDbContext db, IConfiguration config, JwtService jwtService)
    {
        _db = db;
        _config = config;
        _jwtService = jwtService;
    }

    // ── GET /Auth/Login ───────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        return View();
    }

    // ── POST /Auth/Login ──────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Email and password are required.");
            return View();
        }

        var user = await _db.TransactionInquiryUsers
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLower());

        // Generic message - don't reveal whether email exists
        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View();
        }

        // ── Account must be enabled ───────────────────────────────────────
        if (!string.Equals(user.AccountStatus, "enabled", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("", "Your account has been disabled. Please contact your administrator.");
            return View();
        }

        // ── Default-password check -> force change ─────────────────────────
        var defaultPassword = _config["DEFAULT_PASSWORD"];
        if (!string.IsNullOrEmpty(defaultPassword) &&
            BCrypt.Net.BCrypt.Verify(defaultPassword, user.Password))
        {
            TempData["ForceChangeUserId"] = user.Id.ToString();
            return RedirectToAction("UpdatePassword");
        }

        // ── All checks passed — sign the user in ──────────────────────────
        await SignInUserAsync(user);

        // Generate JWT token and store in cookie
        var jwtToken = _jwtService.GenerateToken(user);
        Response.Cookies.Append("jwt_token", jwtToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiryMinutes"] ?? "480"))
        });

        // Update last-login timestamp
        user.LastLoginIP = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _db.SaveChangesAsync();

        await AuditLogger.LogAsync(
            db: _db,
            account: user.Email,
            action: "LOGIN SUCCESSFUL",
            description: $"{user.FirstName} {user.LastName} logged in successfully.",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return RedirectToAction("Index", "Dashboard");
    }

    // ── GET /Auth/UpdatePassword ──────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> UpdatePassword()
    {
        if (TempData["ForceChangeUserId"] is null)
            return RedirectToAction("Login");

        TempData.Keep("ForceChangeUserId");

        var userId = (long)Convert.ToInt64(TempData.Peek("ForceChangeUserId")!);
        var user = await _db.TransactionInquiryUsers.FindAsync(userId);

        if (user is null)
            return RedirectToAction("Login");

        ViewBag.UserName = $"{user.FirstName} {user.LastName}";
        ViewBag.Email = user.Email;
        return View();
    }

    // ── POST /Auth/UpdatePassword ─────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePassword(string oldPassword, string newPassword, string confirmPassword)
    {
        if (TempData["ForceChangeUserId"] is null)
            return RedirectToAction("Login");

        TempData.Keep("ForceChangeUserId");

        var userId = (long)Convert.ToInt64(TempData.Peek("ForceChangeUserId")!);
        var user = await _db.TransactionInquiryUsers.FindAsync(userId);
        if (user is null)
            return RedirectToAction("Login");

        ViewBag.UserName = $"{user.FirstName} {user.LastName}";
        ViewBag.Email = user.Email;

        // ── Validate old password ─────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(oldPassword) || !BCrypt.Net.BCrypt.Verify(oldPassword, user.Password))
        {
            ModelState.AddModelError("", "Current password is incorrect.");
            return View();
        }

        // ── Validate new password ─────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            ModelState.AddModelError("", "New password must be at least 8 characters.");
            return View();
        }

        if (newPassword != confirmPassword)
        {
            ModelState.AddModelError("", "Passwords do not match.");
            return View();
        }

        // ── Prevent reusing the default/temporary password ────────────────
        var defaultPassword = _config["DEFAULT_PASSWORD"];
        if (!string.IsNullOrEmpty(defaultPassword) && newPassword == defaultPassword)
        {
            ModelState.AddModelError("", "You cannot reuse the temporary password. Please choose a new one.");
            return View();
        }

        // ── Save ──────────────────────────────────────────────────────────
        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync();

        await AuditLogger.LogAsync(
            db: _db,
            account: user.Email,
            action: "PASSWORD UPDATED",
            description: $"{user.FirstName} {user.LastName} updated their password.",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        TempData["SuccessMessage"] = "Password updated successfully. Please sign in with your new password.";
        return RedirectToAction("Login");
    }

    // ── GET /Auth/Logout ──────────────────────────────────────────────────
    public async Task<IActionResult> Logout()
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";

        await AuditLogger.LogAsync(
            db: _db,
            account: email,
            action: "LOGOUT",
            description: "User logged out.",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        Response.Cookies.Delete("jwt_token");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    // ── Private helper ────────────────────────────────────────────────────
    private async Task SignInUserAsync(TransactionInquiryUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName),
            new Claim(ClaimTypes.Role, user.AccountType ?? "User"),
            new Claim("privileges", user.Privileges ?? ""),
            new Claim("accountStatus", user.AccountStatus ?? "enabled"),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true });
    }
}
