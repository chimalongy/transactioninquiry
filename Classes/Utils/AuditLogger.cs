using transactioninquiry.Data;
using transactioninquiry.Models;

namespace transactioninquiry.Classes.Utils;

public static class AuditLogger
{
    public static async Task LogAsync(
        AppDbContext db,
        string account,
        string action,
        string? description = null,
        string? ipAddress = null)
    {
        var log = new AuditLog
        {
            Account = account,
            Action = action,
            Description = description,
            IpAddress = ipAddress,
            Time = DateTime.UtcNow
        };

        db.AuditLogs.Add(log);
        await db.SaveChangesAsync();
    }
}
