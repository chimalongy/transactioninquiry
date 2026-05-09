using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace transactioninquiry.Models;

[Table("transactionInquiryUsers")]
public class TransactionInquiryUser
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("firstname")]
    [StringLength(100)]
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Column("lastname")]
    [StringLength(100)]
    [Required]
    public string LastName { get; set; } = string.Empty;

    [Column("email")]
    [StringLength(255)]
    [Required]
    public string Email { get; set; } = string.Empty;

    [Column("password")]
    [StringLength(255)]
    [Required]
    public string Password { get; set; } = string.Empty;

    [Column("accountstatus")]
    [StringLength(50)]
    [Required]
    public string AccountStatus { get; set; } = "enabled";

    [Column("accounttype")]
    [StringLength(50)]
    [Required]
    public string AccountType { get; set; } = "User";

    [Column("privileges")]
    public string? Privileges { get; set; }

    [Column("lastloginip")]
    [StringLength(45)]
    public string? LastLoginIP { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
}
