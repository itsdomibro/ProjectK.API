using Microsoft.EntityFrameworkCore;
using ProjectK.API.Models;

namespace ProjectK.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionDetail> TransactionDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User <-> Cashiers (self-reference)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Cashiers)
                .WithOne(c => c.Owner)
                .HasForeignKey(c => c.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // User <-> Categories
            modelBuilder.Entity<User>()
                .HasMany(u => u.Categories)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User <-> Products
            modelBuilder.Entity<User>()
                .HasMany(u => u.Products)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User <-> Transactions
            modelBuilder.Entity<User>()
                .HasMany(u => u.Transactions)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // Category <-> Products
            modelBuilder.Entity<Category>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Product <-> TransactionDetails
            modelBuilder.Entity<Product>()
                .HasMany(p => p.TransactionDetails)
                .WithOne(td => td.Product)
                .HasForeignKey(td => td.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Transaction <-> TransactionDetails
            modelBuilder.Entity<Transaction>()
                .HasMany(t => t.TransactionDetails)
                .WithOne(td => td.Transaction)
                .HasForeignKey(td => td.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);


            base.OnModelCreating(modelBuilder);
        }
    }
}
