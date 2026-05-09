using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace transactioninquiry.Models;

[Table("scriptquerydatabases")]
public class ScriptDatabase
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("db_type")]
    public string DbType { get; set; } = "postgres";

    [Required]
    [MaxLength(150)]
    [Column("db_name")]
    public string DbName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("host")]
    public string Host { get; set; } = string.Empty;

    [Required]
    [Column("port")]
    public int Port { get; set; } = 5432;

    [Required]
    [MaxLength(150)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [Column("password")]
    public string Password { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by")]
    [MaxLength(255)]
    public string? CreatedBy { get; set; }
}