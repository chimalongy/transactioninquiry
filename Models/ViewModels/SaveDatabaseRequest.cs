namespace transactioninquiry.Models.ViewModels
{
    public class SaveDatabaseRequest
    {
        public string DbType { get; set; } = string.Empty;
        public string DbName { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
