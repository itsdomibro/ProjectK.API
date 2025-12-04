using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectK.API.Data;
using ProjectK.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ProjectK.API.DTOs;


namespace ProjectK.API.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetProduct(
            [FromQuery] string? search,
            [FromQuery] Guid? categoryId)
        {
            
            var ownerId = await GetCurrentOwnerIdAsync();
            var query = _context.Products
            .Where(p => p.UserId == ownerId)
            .AsQueryable();
         
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(search) ||
                    (p.Description != null && p.Description.ToLower().Contains(search))
                );
            }


            // user id di product  = owner id
            var products = await query.Include(p => p.Category).Select(p => new ProductResponseDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Discount = p.Discount,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : null,
                ImageUrl = p.ImageUrl
            }).ToListAsync();
            return Ok(products);
        }

        [HttpPost]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> CreateProduct(CreateProductDto dto)
        {
            var ownerId = await GetCurrentOwnerIdAsync();

            Category? category = null;
            if (dto.CategoryId.HasValue)
            {
                category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId.Value && c.UserId == ownerId);
            }

            var product = new Product
            {
                ProductId = Guid.NewGuid(),
                UserId = ownerId,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Discount = dto.Discount,
                CategoryId = dto.CategoryId,
                ImageUrl = dto.ImageUrl,
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var response = new ProductResponseDto
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Discount = product.Discount,
                CategoryId = product.CategoryId,
                CategoryName = category?.Name,
                ImageUrl = product.ImageUrl
            };

            return Ok(response);

        }

        [HttpPatch("{id}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> UpdateProduct(Guid id, EditProductDto dto)
        {
            var ownerId = await GetCurrentOwnerIdAsync();
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id && p.UserId == ownerId);
            if(product  == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(dto.Name))
                product.Name = dto.Name;

            if (!string.IsNullOrEmpty(dto.Description))
                product.Description = dto.Description;

            if (dto.Price.HasValue)
                product.Price = dto.Price.Value;

            if (dto.Discount.HasValue)
                product.Discount = dto.Discount.Value;

            if (dto.CategoryId != null)
                product.CategoryId = dto.CategoryId;

            if (!string.IsNullOrEmpty(dto.ImageUrl))
                product.ImageUrl = dto.ImageUrl;


            product.UpdatedAt = DateTime.UtcNow;



            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            var ownerId = await GetCurrentOwnerIdAsync();
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id && p.UserId == ownerId);
            if (product == null)
            {
                return NotFound();
            }
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
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
