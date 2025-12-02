namespace ProjectK.API.Models
{
    public class TransactionDetail
    {
        public Guid TransactionDetailId { get; set; } = Guid.NewGuid();
        
        public Guid TransactionId { get; set; }
        public Transaction Transaction { get; set; } = null!;

        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
