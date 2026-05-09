using Microsoft.EntityFrameworkCore;
using transactioninquiry.Data;
using transactioninquiry.Models;
using transactioninquiry.Models.ViewModels;

namespace transactioninquiry.Classes.Utils;

public static class GlobalFunctions
{
    // ══════════════════════════════════════════════════════════════════════
    // USER OPERATIONS
    // ══════════════════════════════════════════════════════════════════════

    public static async Task<IEnumerable<object>> GetAllUsersAsync(AppDbContext db)
    {
        return await db.TransactionInquiryUsers
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.AccountStatus,
                u.AccountType,
                u.Privileges,
                u.LastLoginIP,
                u.CreatedAt
            })
            .ToListAsync();
    }

    public static async Task<(object? Result, string? Error)> CreateUserAsync(
        AppDbContext db,
        IConfiguration config,
        CreateUserRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FirstName))
            return (null, "First name is required.");

        if (string.IsNullOrWhiteSpace(req.LastName))
            return (null, "Last name is required.");

        if (string.IsNullOrWhiteSpace(req.Email))
            return (null, "Email is required.");

        bool emailExists = await db.TransactionInquiryUsers
            .CountAsync(u => u.Email.ToLower() == req.Email.Trim().ToLower()) > 0;

        if (emailExists)
            return (null, "CONFLICT");

        var defaultPassword = config["DEFAULT_PASSWORD"] ?? "P@ssw0rd123!";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(defaultPassword);

        var user = new TransactionInquiryUser
        {
            FirstName = req.FirstName.Trim(),
            LastName = req.LastName.Trim(),
            Email = req.Email.Trim().ToLower(),
            AccountStatus = "enabled",
            AccountType = req.AccountType?.Trim() ?? "User",
            Privileges = req.Privileges?.Trim(),
            Password = hashedPassword,
            CreatedAt = DateTime.UtcNow
        };

        db.TransactionInquiryUsers.Add(user);
        await db.SaveChangesAsync();

        return (new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.AccountStatus,
            user.AccountType,
            user.Privileges,
            user.LastLoginIP,
            user.CreatedAt
        }, null);
    }

    public static async Task<string?> DeleteUserAsync(AppDbContext db, long id)
    {
        var user = await db.TransactionInquiryUsers.FindAsync(id);
        if (user is null)
            return "NOT_FOUND";

        db.TransactionInquiryUsers.Remove(user);
        await db.SaveChangesAsync();
        return null;
    }

    public static async Task<(object? Result, string? Error)> UpdateUserStatusAsync(
        AppDbContext db,
        long id,
        string? newStatus)
    {
        var allowed = new[] { "enabled", "disabled" };
        if (!allowed.Contains(newStatus?.ToLower()))
            return (null, "Status must be 'enabled' or 'disabled'.");

        var user = await db.TransactionInquiryUsers.FindAsync(id);
        if (user is null)
            return (null, "NOT_FOUND");

        user.AccountStatus = newStatus!.ToLower();
        await db.SaveChangesAsync();

        return (new { user.Id, user.AccountStatus }, null);
    }

    public static async Task<(object? Result, string? Error)> UpdateUserAsync(
        AppDbContext db,
        long id,
        CreateUserRequest req)
    {
        var user = await db.TransactionInquiryUsers.FindAsync(id);
        if (user is null)
            return (null, "NOT_FOUND");

        if (!string.IsNullOrWhiteSpace(req.FirstName))
            user.FirstName = req.FirstName.Trim();
        if (!string.IsNullOrWhiteSpace(req.LastName))
            user.LastName = req.LastName.Trim();
        if (!string.IsNullOrWhiteSpace(req.Email))
            user.Email = req.Email.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(req.AccountType))
            user.AccountType = req.AccountType.Trim();
        if (req.Privileges != null)
            user.Privileges = req.Privileges.Trim();

        await db.SaveChangesAsync();

        return (new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.AccountStatus,
            user.AccountType,
            user.Privileges,
            user.LastLoginIP,
            user.CreatedAt
        }, null);
    }

    // ══════════════════════════════════════════════════════════════════════
    // AUDIT LOG OPERATIONS
    // ══════════════════════════════════════════════════════════════════════

    public static async Task<IEnumerable<object>> GetAllAuditLogsAsync(AppDbContext db)
    {
        return await db.AuditLogs
            .OrderByDescending(l => l.Time)
            .Select(l => new
            {
                l.Id,
                l.Account,
                l.Action,
                l.Description,
                l.IpAddress,
                l.Time
            })
            .ToListAsync();
    }
}
