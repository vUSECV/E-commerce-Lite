namespace OrderService.Models
{
    public class PaymentResult
    {
        public bool Success { get; set; }
        public int PaymentId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
