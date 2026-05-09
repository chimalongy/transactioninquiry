using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using transactioninquiry.Classes.Utils;
using transactioninquiry.Data;
using transactioninquiry.Models.ViewModels;
using Npgsql;
using Oracle.ManagedDataAccess.Client; // dotnet add package Oracle.ManagedDataAccess.Core
using System.Text;
    using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using transactioninquiry.Classes.Utils;
using transactioninquiry.Data;
using transactioninquiry.Models;
using transactioninquiry.Models.ViewModels;

namespace transactioninquiry.Controllers;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class DashboardController : Controller
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public DashboardController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }


    // ── Current user info ─────────────────────────────────────────────────
    private async Task<(string FirstName, string LastName, string Email, string AccountType, string? Privileges)>
        GetCurrentUserAsync()
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? "";
        var user = await _db.TransactionInquiryUsers
            .FirstOrDefaultAsync(u => u.Email == email);

        return (
            user?.FirstName ?? User.FindFirstValue(ClaimTypes.GivenName) ?? "",
            user?.LastName ?? User.FindFirstValue(ClaimTypes.Surname) ?? "",
            email,
            user?.AccountType ?? User.FindFirstValue(ClaimTypes.Role) ?? "User",
            user?.Privileges ?? User.FindFirst("privileges")?.Value
        );
    }

    // ══════════════════════════════════════════════════════════════════════
    // VIEWS
    // ══════════════════════════════════════════════════════════════════════

    [HttpGet]
    public IActionResult Index()
    {
        return View("~/Views/Dashboard/Index.cshtml");
    }

    [HttpGet]
    [Route("Dashboard/Swglobal")]
    public IActionResult Swglobal()
    {
        return View("~/Views/Dashboard/Swglobal.cshtml");
    }

    [HttpGet]
    [Route("Dashboard/SettlementReports")]
    public IActionResult SettlementReports()
    {
        return View("~/Views/Dashboard/SettlementReports.cshtml");
    }

    [HttpGet]
    [Route("Dashboard/Scripts")]
    public IActionResult Scripts()
    {
        return View("~/Views/Dashboard/Scripts.cshtml");
    }

    [HttpGet]
    [Route("Dashboard/ManualSettlements")]
    public IActionResult ManualSettlements()
    {
        return View("~/Views/Dashboard/ManualSettlements.cshtml");
    }

    [HttpGet]
    [Route("Dashboard/ExceptionReports")]
    public IActionResult ExceptionReports()
    {
        return View("~/Views/Dashboard/ExceptionReports.cshtml");
    }

    [HttpGet]
    [Route("Dashboard/Users")]
    public IActionResult Users()
    {
        return View("~/Views/Dashboard/Users.cshtml");
    }

    [HttpGet]
    [Route("Dashboard/Audit")]
    public IActionResult Audit()
    {
        return View("~/Views/Dashboard/Audit.cshtml");
    }

    // ══════════════════════════════════════════════════════════════════════
    // USERS API
    // ══════════════════════════════════════════════════════════════════════

    [HttpGet]
    [Route("Dashboard/Users/GetAll")]
    public async Task<IActionResult> UsersGetAll()
    {
        var users = await GlobalFunctions.GetAllUsersAsync(_db);
        return Json(users);
    }

    [HttpPost]
    [Route("Dashboard/Users/Create")]
    public async Task<IActionResult> UsersCreate([FromBody] CreateUserRequest req)
    {
        var (result, error) = await GlobalFunctions.CreateUserAsync(_db, _config, req);

        if (error == "CONFLICT")
            return Conflict(new { message = "A user with this email already exists." });

        if (error is not null)
            return BadRequest(new { message = error });

        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

        await AuditLogger.LogAsync(
            db: _db,
            account: userEmail,
            action: "CREATE USER",
            description: $"{userName} created user: {req.FirstName} {req.LastName} ({req.Email})",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return Json(result);
    }

    [HttpPost]
    [Route("Dashboard/Users/Update/{id:long}")]
    public async Task<IActionResult> UsersUpdate(long id, [FromBody] CreateUserRequest req)
    {
        var (result, error) = await GlobalFunctions.UpdateUserAsync(_db, id, req);

        if (error == "NOT_FOUND")
            return NotFound(new { message = "User not found." });

        if (error is not null)
            return BadRequest(new { message = error });

        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

        await AuditLogger.LogAsync(
            db: _db,
            account: userEmail,
            action: "UPDATE USER",
            description: $"{userName} updated user ID: {id}",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return Json(result);
    }

    [HttpPost]
    [Route("Dashboard/Users/Delete/{id:long}")]
    public async Task<IActionResult> UsersDelete(long id)
    {
        var userToDelete = await _db.TransactionInquiryUsers.FindAsync(id);
        var userName = userToDelete != null ? $"{userToDelete.FirstName} {userToDelete.LastName}" : $"ID {id}";

        var error = await GlobalFunctions.DeleteUserAsync(_db, id);

        if (error == "NOT_FOUND")
            return NotFound(new { message = "User not found." });

        var adminEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";
        var adminName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

        await AuditLogger.LogAsync(
            db: _db,
            account: adminEmail,
            action: "DELETE USER",
            description: $"{adminName} deleted user: {userName} (ID: {id})",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return Json(new { message = "User deleted." });
    }

    [HttpPost]
    [Route("Dashboard/Users/UpdateStatus/{id:long}")]
    public async Task<IActionResult> UsersUpdateStatus(long id, [FromBody] UpdateStatusRequest req)
    {
        var (result, error) = await GlobalFunctions.UpdateUserStatusAsync(_db, id, req.Status);

        if (error == "NOT_FOUND")
            return NotFound(new { message = "User not found." });

        if (error is not null)
            return BadRequest(new { message = error });

        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

        await AuditLogger.LogAsync(
            db: _db,
            account: userEmail,
            action: "UPDATE USER STATUS",
            description: $"{userName} updated user ID: {id} status to {req.Status}",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return Json(result);
    }

    // ══════════════════════════════════════════════════════════════════════
    // AUDIT LOGS API
    // ══════════════════════════════════════════════════════════════════════

    [HttpGet]
    [Route("Dashboard/Audit/GetAll")]
    public async Task<IActionResult> AuditLogsGetAll()
    {
        var logs = await GlobalFunctions.GetAllAuditLogsAsync(_db);
        return Json(logs);
    }




    // ════════════════════════════════════════════════════════════════════
    // SCRIPTS API
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Returns all saved database connections (passwords omitted).</summary>
    [HttpGet]
    [Route("Dashboard/Scripts/GetDatabases")]
    public async Task<IActionResult> ScriptsGetDatabases()
    {
        try
        {
            var dbs = await _db.ScriptDatabases
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new
                {
                    d.Id,
                    d.DbType,
                    d.DbName,
                    d.Host,
                    d.Port,
                    d.Username,
                    d.CreatedAt
                })
                .ToListAsync();

            return Json(dbs);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to load databases.", detail = ex.Message });
        }
    }

    /// <summary>Tests a database connection without saving it.</summary>
    [HttpPost]
    [Route("Dashboard/Scripts/TestConnection")]
    public async Task<IActionResult> ScriptsTestConnection([FromBody] TestConnectionRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.DbName) ||
            string.IsNullOrWhiteSpace(req.Host) ||
            string.IsNullOrWhiteSpace(req.Username) ||
            string.IsNullOrWhiteSpace(req.Password) ||
            req.Port is <= 0 or > 65535)
        {
            return BadRequest(new { success = false, message = "All fields are required and port must be valid (1–65535)." });
        }

        try
        {
            await TestConnectionInternalAsync(req.DbType, req.Host, req.Port, req.DbName, req.Username, req.Password);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>Saves a verified database connection (password encrypted).</summary>
    [HttpPost]
    [Route("Dashboard/Scripts/SaveDatabase")]
    public async Task<IActionResult> ScriptsSaveDatabase([FromBody] SaveDatabaseRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.DbName) ||
            string.IsNullOrWhiteSpace(req.Host) ||
            string.IsNullOrWhiteSpace(req.Username) ||
            string.IsNullOrWhiteSpace(req.Password) ||
            req.Port is <= 0 or > 65535)
        {
            return BadRequest(new { message = "All fields are required and port must be valid (1–65535)." });
        }

        try
        {
            await TestConnectionInternalAsync(req.DbType, req.Host, req.Port, req.DbName, req.Username, req.Password);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Connection test failed: " + ex.Message });
        }

        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";

        var entry = new ScriptDatabase
        {
            DbType = req.DbType.ToLower(),
            DbName = req.DbName.Trim(),
            Host = req.Host.Trim(),
            Port = req.Port,
            Username = req.Username.Trim(),
            Password = Cryptor.Encrypt(req.Password, true),
            CreatedBy = userEmail,
            CreatedAt = DateTime.UtcNow
        };

        _db.ScriptDatabases.Add(entry);
        await _db.SaveChangesAsync();

        await AuditLogger.LogAsync(
            db: _db,
            account: userEmail,
            action: "CREATE SCRIPT DB",
            description: $"{userEmail} saved database connection: {entry.DbName} ({entry.DbType}) @ {entry.Host}:{entry.Port}",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return Json(new
        {
            entry.Id,
            entry.DbType,
            entry.DbName,
            entry.Host,
            entry.Port,
            entry.Username,
            entry.CreatedAt
        });
    }

    /// <summary>Runs a read-only SQL query against a saved database.</summary>
    [HttpPost]
    [Route("Dashboard/Scripts/RunQuery")]
    public async Task<IActionResult> ScriptsRunQuery([FromBody] RunQueryRequest req)
    {
        if (req.DatabaseId <= 0 || string.IsNullOrWhiteSpace(req.Query))
            return BadRequest(new { message = "Database and query are required." });

        var dbEntry = await _db.ScriptDatabases.FindAsync(req.DatabaseId);
        if (dbEntry is null)
            return NotFound(new { message = "Database not found." });

        var password = Cryptor.Decrypt(dbEntry.Password, true);

        try
        {
            var (columns, rows) = await ExecuteQueryAsync(
                dbEntry.DbType, dbEntry.Host, dbEntry.Port, dbEntry.DbName,
                dbEntry.Username, password, req.Query);

            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";
            await AuditLogger.LogAsync(
                db: _db,
                account: userEmail,
                action: "RUN SCRIPT QUERY",
                description: $"{userEmail} ran query on {dbEntry.DbName} ({dbEntry.DbType}): " +
                             req.Query[..Math.Min(120, req.Query.Length)],
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            return Json(new { columns, rows });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }


    // ── Internal helpers ──────────────────────────────────────────────────

    private static async Task TestConnectionInternalAsync(
        string dbType, string host, int port, string dbName, string username, string password)
    {
        switch (dbType.ToLower())
        {
            case "postgres":
                {
                    var cs = BuildPostgresConnectionString(host, port, dbName, username, password);
                    await using var conn = new NpgsqlConnection(cs);
                    await conn.OpenAsync();
                    break;
                }
            case "oracle":
                {
                    var cs = BuildOracleConnectionString(host, port, dbName, username, password);
                    await using var conn = new OracleConnection(cs);
                    await conn.OpenAsync();
                    break;
                }
            default:
                throw new ArgumentException("Unsupported database type: " + dbType);
        }
    }

    private static async Task<(List<string> Columns, List<Dictionary<string, object?>> Rows)>
        ExecuteQueryAsync(string dbType, string host, int port, string dbName,
                          string username, string password, string sql)
    {
        var columns = new List<string>();
        var rows = new List<Dictionary<string, object?>>();

        switch (dbType.ToLower())
        {
            case "postgres":
                {
                    var cs = BuildPostgresConnectionString(host, port, dbName, username, password);
                    await using var conn = new NpgsqlConnection(cs);
                    await conn.OpenAsync();
                    await using var cmd = new NpgsqlCommand(sql, conn) { CommandTimeout = 30 };
                    await using var reader = await cmd.ExecuteReaderAsync();
                    ReadResults(reader, columns, rows);
                    break;
                }
            case "oracle":
                {
                    var cs = BuildOracleConnectionString(host, port, dbName, username, password);
                    await using var conn = new OracleConnection(cs);
                    await conn.OpenAsync();
                    await using var cmd = new OracleCommand(sql, conn) { CommandTimeout = 30 };
                    await using var reader = await cmd.ExecuteReaderAsync();
                    ReadResults(reader, columns, rows);
                    break;
                }
            default:
                throw new ArgumentException("Unsupported database type: " + dbType);
        }

        return (columns, rows);
    }

    private static void ReadResults(IDataReader reader,
        List<string> columns, List<Dictionary<string, object?>> rows)
    {
        for (var i = 0; i < reader.FieldCount; i++)
            columns.Add(reader.GetName(i));

        while (reader.Read())
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
                row[columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i)?.ToString();
            rows.Add(row);
        }
    }

    // Port is now a dedicated model field — no host-splitting needed.
    private static string BuildPostgresConnectionString(
        string host, int port, string dbName, string username, string password)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            Database = dbName,
            Username = username,
            Password = password,
            Timeout = 10,
            SslMode = SslMode.Prefer
        };
        return builder.ToString();
    }

    private static string BuildOracleConnectionString(
        string host, int port, string dbName, string username, string password)
    {
        return $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port}))" +
               $"(CONNECT_DATA=(SERVICE_NAME={dbName})));User Id={username};Password={password};";
    }

    // ── Password encryption ───────────────────────────────────────────────

    private string EncryptPassword(string plain)
    {
        var key = GetEncryptionKey();
        var bytes = System.Text.Encoding.UTF8.GetBytes(plain);
        var enc = System.Security.Cryptography.Aes.Create();
        enc.Key = key;
        enc.GenerateIV();
        using var ms = new System.IO.MemoryStream();
        ms.Write(enc.IV, 0, enc.IV.Length);
        using var cs = new System.Security.Cryptography.CryptoStream(
            ms, enc.CreateEncryptor(), System.Security.Cryptography.CryptoStreamMode.Write);
        cs.Write(bytes, 0, bytes.Length);
        cs.FlushFinalBlock();
        return Convert.ToBase64String(ms.ToArray());
    }

    private string DecryptPassword(string cipher)
    {
        var key = GetEncryptionKey();
        var full = Convert.FromBase64String(cipher);
        var iv = full[..16];
        var data = full[16..];
        var dec = System.Security.Cryptography.Aes.Create();
        dec.Key = key;
        dec.IV = iv;
        using var ms = new System.IO.MemoryStream(data);
        using var cs = new System.Security.Cryptography.CryptoStream(
            ms, dec.CreateDecryptor(), System.Security.Cryptography.CryptoStreamMode.Read);
        using var sr = new System.IO.StreamReader(cs);
        return sr.ReadToEnd();
    }

    private byte[] GetEncryptionKey()
    {
        var b64 = _config["Encryption:Key"]
            ?? throw new InvalidOperationException(
                "Encryption:Key is not configured. Add a 32-byte base64 key to appsettings.json.");
        var key = Convert.FromBase64String(b64);
        if (key.Length != 32)
            throw new InvalidOperationException("Encryption:Key must be exactly 32 bytes (256-bit).");
        return key;
    }

    /// <summary>Strips internal connection-string details from DB errors before sending to client.</summary>
    private static string SanitizeDbError(string message)
    {
        if (message.Contains("password") || message.Contains("authentication"))
            return "Authentication failed. Please check your username and password.";
        if (message.Contains("timeout") || message.Contains("connect"))
            return "Could not reach the database host. Please check the host address and port.";
        if (message.Contains("database") && message.Contains("exist"))
            return "Database not found on the specified host.";
        return "Connection failed. Please verify your connection details.";
    }





























}
