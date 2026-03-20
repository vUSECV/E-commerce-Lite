namespace PaymentService.DTOs
{
    public class ProcessPaymentDto
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
    }
}
