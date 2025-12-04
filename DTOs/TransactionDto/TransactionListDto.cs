namespace ProjectK.API.DTOs.TransactionDto
{
    public class TransactionListDto
    {
        public Guid TransactionId { get; set; }
        public string Code { get; set; }
        public string Payment { get; set; }
        public bool IsPaid { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public List<TransactionDetailListDto> Details { get; set; } = new();
    }

}
