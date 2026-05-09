namespace transactioninquiry.Models.ViewModels;

public class CreateUserRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AccountType { get; set; } = "User";
    public string? Privileges { get; set; }
}
