using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectK.API.Data;
using System.Security.Claims;
using ProjectK.API.DTOs.TransactionDto;
using ProjectK.API.Models;
using Microsoft.AspNetCore.Identity;


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
        public async Task<IActionResult> GetAll([FromQuery] TransactionFilterDto filter)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // Get ownerId for both owner & cashier
            var ownerId = await GetCurrentOwnerIdAsync();


            // Base query (ambil transaction + detail + product)
            var query = _context.Transactions
                .Where(t => t.UserId == ownerId)
                .Include(t => t.TransactionDetails)
                    .ThenInclude(td => td.Product)
                .AsQueryable();

            // Cashier restriction
            if (role == "Cashier")
            {
                var today = DateTime.UtcNow.Date;
                query = query.Where(t => t.CreatedAt.Date == today);
            }

            // --- Filtering ---
            //logic search
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string search = filter.Search.ToLower();

                // logic search lah by transaction id , payment dan productname
                query = query.Where(t =>
                    t.TransactionId.ToString().ToLower().Contains(search) || 
                    t.Payment.ToLower().Contains(search) || 
                    t.TransactionDetails.Any(td => td.Product.Name.ToLower().Contains(search)));
            }

            // filter by ispaid & payment
            if (filter.IsPaid.HasValue)
                query = query.Where(t => t.IsPaid == filter.IsPaid.Value);

            if (!string.IsNullOrEmpty(filter.Payment))
                query = query.Where(t => t.Payment == filter.Payment);

            // --- Sorting ---
            switch (filter.SortBy)
            {
                case "amount":
                    query = filter.SortOrder == "asc"
                        ? query.OrderBy(t => t.TransactionDetails.Sum(d => d.Quantity * d.Product.Price))
                        : query.OrderByDescending(t => t.TransactionDetails.Sum(d => d.Quantity * d.Product.Price));
                    break;

                default: // date (KALO GA ADA OTOMATIS DATE BY DESC)
                    query = filter.SortOrder == "asc"
                        ? query.OrderBy(t => t.CreatedAt)
                        : query.OrderByDescending(t => t.CreatedAt);
                    break;
            }

            // ---  ---
            var list = await query.ToListAsync();

            // --- Mapping to DTO ---
            var result = list.Select(t => new TransactionListDto
            {
                TransactionId = t.TransactionId,
                Code = t.Code,
                Payment = t.Payment,
                IsPaid = t.IsPaid,
                CreatedAt = t.CreatedAt,
                TotalAmount = t.TransactionDetails.Sum(d => d.Quantity * d.Product.Price),

                Details = t.TransactionDetails.Select(d => new TransactionDetailListDto
                {
                    ProductId = d.ProductId,
                    ProductName = d.Product.Name,
                    Quantity = d.Quantity,
                    Price = d.Product.Price
                }).ToList()
            }).ToList();

            return Ok(result);
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
        public async Task<IActionResult> GetById(Guid id)
        {
            var ownerId = await GetCurrentOwnerIdAsync();
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var query = _context.Transactions
                .Where(t => t.UserId == ownerId)
                .Include(t => t.TransactionDetails)
                    .ThenInclude(td => td.Product)
                .AsQueryable();

            if(role == "Cashier")
            {
                var today = DateTime.UtcNow;
                query = query.Where(t => t.CreatedAt.Date == today);
            }

            var transaction = await query.FirstOrDefaultAsync(t => t.TransactionId == id);
            if(transaction == null)
            {
                return NotFound();
            }

            var result = new TransactionListDto
            {
                TransactionId = transaction.TransactionId,
                Code = transaction.Code,
                Payment = transaction.Payment,
                CreatedAt = transaction.CreatedAt,
                TotalAmount = transaction.TransactionDetails.Sum(d => d.Quantity * d.Product.Price),

                Details = transaction.TransactionDetails.Select(d => new TransactionDetailListDto
                {
                    ProductId = d.ProductId,
                    ProductName = d.Product.Name,
                    Quantity = d.Quantity,
                    Price = d.Product.Price
                }).ToList()
            };
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ownerId = await GetCurrentOwnerIdAsync();
            var transaction = await _context.Transactions
                                            .Where(t => t.UserId == ownerId)
                                            .Include(t => t.TransactionDetails)
                                            .FirstOrDefaultAsync(t => t.TransactionId == id);
            if(transaction == null)
            {
                return NotFound();
            }
            _context.TransactionDetails.RemoveRange(transaction.TransactionDetails);
            _context.Transactions.Remove(transaction);

            await _context.SaveChangesAsync();
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
