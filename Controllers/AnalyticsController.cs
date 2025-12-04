using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectK.API.Data;
using System.Security.Claims;

namespace ProjectK.API.Controllers
{
    public record RevenueResponseDto(decimal Total, string Currency, DateTime Start, DateTime End);
    public record TopProductDto(Guid ProductId, string Name, int QuantitySold, decimal Revenue);

    [Route("api/analytics")]
    [Authorize(Roles = "Owner")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        private readonly AppDbContext _context;
        
        public AnalyticsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenue([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var ownerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(ownerIdStr, out var ownerId)) return Unauthorized();

            var total = await _context.Transactions
                .Where(t => t.UserId == ownerId && t.CreatedAt >= start && t.CreatedAt <= end && !t.IsDeleted)
                .SumAsync(t => t.IsPaid ? t.TransactionDetails.Sum(d => d.Quantity * d.Product.Price) : 0);

            return Ok(new RevenueResponseDto(total, "IDR", start, end));
        }

        [HttpGet("top-products")]
        public async Task<IActionResult> GetTopProducts([FromQuery] DateTime start, [FromQuery] DateTime end, [FromQuery] int take = 5)
        {
            var ownerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(ownerIdStr, out var ownerId)) return Unauthorized();

            var top = await _context.TransactionDetails
                .Where(i => i.Transaction.UserId == ownerId && i.Transaction.CreatedAt >= start && i.Transaction.CreatedAt <= end && !i.Transaction.IsDeleted)
                .GroupBy(i => new { i.ProductId, i.Product.Name })
                .Select(g => new TopProductDto(
                    g.Key.ProductId,
                    g.Key.Name,
                    g.Sum(x => x.Quantity),
                    g.Sum(x => x.Quantity * x.Product.Price)))
                .OrderByDescending(p => p.Revenue)
                .Take(take)
                .ToListAsync();

            return Ok(top);
        }

    }
}
