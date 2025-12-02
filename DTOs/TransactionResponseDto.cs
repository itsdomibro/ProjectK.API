namespace ProjectK.API.DTOs
{
    public class TransactionResponseDto
    {
        public Guid TransactionId { get; set; }
        public bool IsPaid { get; set; }
        public string PaymentName { get; set; }
        public string Code { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
