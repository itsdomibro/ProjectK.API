
namespace ProjectK.API.Models
{
    public class User
    {
        public Guid UserId { get; set; } = Guid.NewGuid();
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public string? BusinessDescription { get; set; }
        public string Role { get; set; } = string.Empty;

        public Guid? OwnerId { get; set; }
        public User? Owner { get; set; }

        public ICollection<User> Cashiers { get; set; } = new List<User>();

        public bool IsDeactivated { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
