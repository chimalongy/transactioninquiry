using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using transactioninquiry.Classes.Utils;
using transactioninquiry.Data;

var builder = WebApplication.CreateBuilder(args);

// ── Decrypt and validate connection string ──────────────────────────────────
string? encryptedConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//string connString = "Host=localhost;Port=5432;Database=transactioninquiry;Username=postgres;Password=1;";

//string encryptedConnString = Cryptor.Encrypt(connString, true);

if (string.IsNullOrEmpty(encryptedConnectionString))
    throw new Exception("Connection string 'DefaultConnection' not found.");

string decryptedConnectionString = Cryptor.Decrypt(encryptedConnectionString, true);

if (!decryptedConnectionString.Contains("Host", StringComparison.OrdinalIgnoreCase) &&
    !decryptedConnectionString.Contains("Server", StringComparison.OrdinalIgnoreCase))
    throw new Exception("Invalid PostgreSQL connection string. Expected 'Host' or 'Server' parameter.");

// ── Database (PostgreSQL) ───────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(decryptedConnectionString));

// ── JWT ─────────────────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new Exception("JWT Key not configured.");

builder.Services.AddSingleton<JwtService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/Login";
    options.Cookie.Name = "TransactionInquiry";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(
        int.Parse(builder.Configuration["Jwt:ExpiryMinutes"] ?? "480"));
    options.SlidingExpiration = true;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.ContainsKey("jwt_token"))
            {
                context.Token = context.Request.Cookies["jwt_token"];
            }
            return Task.CompletedTask;
        }
    };
});

// ── Authorization ───────────────────────────────────────────────────────────
builder.Services.AddAuthorization();

// ── MVC ─────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ── Middleware Pipeline ─────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

// ── Auto-migrate on startup ─────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Seed admin user if none exists
    var adminExists = db.TransactionInquiryUsers.Any(u => u.AccountType == "Admin");
    if (!adminExists)
    {
        var defaultPassword = builder.Configuration["DEFAULT_PASSWORD"] ?? "P@ssw0rd123!";
        var admin = new transactioninquiry.Models.TransactionInquiryUser
        {
            FirstName = "System",
            LastName = "Administrator",
            Email = "admin@transactioninquiry.com",
            Password = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
            AccountStatus = "enabled",
            AccountType = "Admin",
            Privileges = "ALL",
            CreatedAt = DateTime.UtcNow
        };
        db.TransactionInquiryUsers.Add(admin);
        db.SaveChanges();
    }
}

app.Run();
