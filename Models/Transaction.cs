
namespace ProjectK.API.Models
{
    public class Transaction
    {
        public Guid TransactionId { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public bool IsPaid { get; set; } = false;

        public Guid PaymentId { get; set; }
        public Payment Payment { get; set; } = null!;

        public string Code { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        public ICollection<TransactionDetail> TransactionDetails { get; set; } = new List<TransactionDetail>();
    }
}
