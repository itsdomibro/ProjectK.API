using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectK.API.Data;
using System.Security.Claims;
using ProjectK.API.DTOs.TransactionDto;
using ProjectK.API.Models;


namespace ProjectK.API.Controllers
{
    [Route("api/transactions")]
    [ApiController]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TransactionsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            
        }
        [HttpPost]
        public async Task<IActionResult> Create(CreateTransactionDto dto)
        {
            var ownerId = await GetCurrentOwnerIdAsync();
            if(dto.Items == null  || !dto.Items.Any())
            {
                return BadRequest("Transaction must have at least one item.");
            }

            var productIds = dto.Items
                .Select(i => i.ProductId)
                .Distinct()
                .ToList();

            var validProductIds = await _context.Products
                .Where(p => p.UserId == ownerId && productIds.Contains(p.ProductId))
                .Select(p => p.ProductId)
                .ToListAsync();

            if (validProductIds.Count != productIds.Count)
            {
                return BadRequest("One or more product IDs are invalid or not owned by this user.");
            }

            var transaction = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                UserId = ownerId,
                Payment = dto.Payment,
                Code = "SKADKAW", // Generate code logic here
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                IsPaid = false,
                IsDeleted = false
            };

            _context.Transactions.Add(transaction);
            
            foreach (var item in dto.Items)
            {
                var transactionDetail = new TransactionDetail
                {
                    TransactionDetailId = Guid.NewGuid(),
                    TransactionId = transaction.TransactionId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    CreatedAt = DateTime.UtcNow,
                };
                _context.TransactionDetails.Add(transactionDetail);
            }

            await _context.SaveChangesAsync();

            return Ok(new TransactionResponseDto
            {
                TransactionId = transaction.TransactionId,
                IsPaid = transaction.IsPaid,
                PaymentName = transaction.Payment,
                Code = transaction.Code,
                Timestamp = transaction.CreatedAt
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById()
        {
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete()
        {
            return Ok();
        }

        private async Task<Guid> GetCurrentOwnerIdAsync()
        {

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Invalid token.");

            var currentUserId = Guid.Parse(userId);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == currentUserId);

            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            if (role == "Owner")
            {
                return user.UserId;
            }

            if (role == "Cashier")
            {
                return user.OwnerId ?? Guid.Empty;
            }

            throw new UnauthorizedAccessException("Role not supported");
        }
    }
}
