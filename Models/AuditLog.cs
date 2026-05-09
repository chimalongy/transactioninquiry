using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace transactioninquiry.Models;

[Table("auditLogs")]
public class AuditLog
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("account")]
    [StringLength(255)]
    [Required]
    public string Account { get; set; } = string.Empty;

    [Column("action")]
    [StringLength(100)]
    [Required]
    public string Action { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("ipaddress")]
    [StringLength(45)]
    public string? IpAddress { get; set; }

    [Column("time")]
    public DateTime Time { get; set; } = DateTime.UtcNow;
}
