namespace transactioninquiry.Models.ViewModels;

public class JwtLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
