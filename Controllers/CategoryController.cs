using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectK.API.Data;
using ProjectK.API.Models;
using System.Security.Claims;
using ProjectK.API.DTOs;
using System.Runtime.InteropServices;

namespace ProjectK.API.Controllers
{
    [Route("api/category")]
    [ApiController]
    [Authorize]
    public class CategoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var ownerId = await GetCurrentOwnerIdAsync();

            var categories = await _context.Categories
                .Where(c => c.UserId == ownerId).Select(c => new CategoryResponseDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    Description = c.Description
                }).ToListAsync();
            return Ok(categories);
        }


        // ------------------------------------------------------------------
        // POST: api/categories
        // Hanya Owner yang boleh membuat Category
        // ------------------------------------------------------------------
        [HttpPost]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> CreateCategory(CreateCategoryDto dto)
        {
            var userId = await GetCurrentOwnerIdAsync();
            if (await _context.Categories.AnyAsync(c => c.Name == dto.Name && c.UserId == userId)) {
                return BadRequest("Category name can't have duplicate");
            }

            var category = new Category
            {
                CategoryId = Guid.NewGuid(),
                UserId = userId,
                Name = dto.Name,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategories), new { id = category.CategoryId }, new CategoryResponseDto
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                Description = category.Description
            });
        }

        // ------------------------------------------------------------------
        // PATCH: api/categories/{id}
        // Hanya Owner yang boleh update Category (harus punya category tersebut)
        // ------------------------------------------------------------------
        [HttpPatch("{id}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> UpdateCategory(Guid id, EditCategoryDto dto)
        {
            var ownerId = await GetCurrentOwnerIdAsync();
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == id && c.UserId == ownerId);
            if (category == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(dto.Name))
                category.Name = dto.Name;

            if (!string.IsNullOrEmpty(dto.Description))
                category.Description = dto.Description;

            category.Description = dto.Description;
            category.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ------------------------------------------------------------------
        // DELETE: api/categories/{id}
        // Hanya Owner yang boleh delete Category (harus punya category tersebut)
        // ------------------------------------------------------------------
        [HttpDelete("{id}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var ownerId = await GetCurrentOwnerIdAsync();
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == id && c.UserId == ownerId);
            if (category == null)
            {
                return NotFound();
            }
            var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);
            if (hasProducts)
            {
                return BadRequest("Cannot delete category with associated products.");
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        // masi bisa di refactor
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
