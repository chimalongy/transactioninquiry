namespace transactioninquiry.Models.ViewModels
{
    public class RunQueryRequest
    {
        public int DatabaseId { get; set; }
        public string Query { get; set; } = string.Empty;
    }
}
