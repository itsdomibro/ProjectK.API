using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectK.API.Data;
using ProjectK.API.DTOs;
using ProjectK.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ProjectK.API.Controllers
{
    [Route("api/cashiers")]
    [Authorize(Roles = "Owner")]
    [ApiController]
    public class CashierController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CashierController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out var userId)) {
                return Unauthorized();
            }

            var cashiers = await _context.Users
                .Where(u => u.Role == "Cashier" && u.OwnerId == userId)
                .Select(u => new CashierResponseDto {
                    UserId = u.UserId,
                    UserName = u.UserName,
                    Email = u.Email,
                    IsDeactivated = u.IsDeactivated,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt ?? u.CreatedAt
                })
                .ToListAsync();

            return Ok(cashiers);
        }


        [HttpPost]
        public async Task<IActionResult> Create(CashierCreateDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out var userId)) {
                return Unauthorized();
            }

            var cashier = new User {
                UserName = dto.UserName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Cashier",
                OwnerId = Guid.Parse(userIdString)
            };

            _context.Users.Add(cashier);

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAll), new { cashierId = cashier.UserId },
                new CashierResponseDto {
                    UserId = cashier.UserId,
                    UserName = cashier.UserName,
                    Email = cashier.Email,
                    IsDeactivated = cashier.IsDeactivated,
                    CreatedAt = cashier.CreatedAt,
                    UpdatedAt = cashier.UpdatedAt ?? DateTime.UtcNow
                });
        }


        [HttpPatch("{cashierId}")]
        public async Task<IActionResult> Edit(Guid cashierId, [FromBody] CashierEditDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out var userId))
                return Unauthorized();

            var cashier = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == cashierId && u.OwnerId == userId && u.Role == "Cashier");
            if (cashier == null)
                return NotFound("Cashier not found or not owned by you."); 

            if (!string.IsNullOrEmpty(dto.UserName))
                cashier.UserName = dto.UserName;

            if (!string.IsNullOrEmpty(dto.Email))
                cashier.Email = dto.Email;

            if (!string.IsNullOrEmpty(dto.Password))
                cashier.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            if (dto.IsDeactivated.HasValue)
                cashier.IsDeactivated = dto.IsDeactivated.Value;

            cashier.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new CashierResponseDto {
                UserId = cashier.UserId,
                UserName = cashier.UserName,
                Email = cashier.Email,
                IsDeactivated = cashier.IsDeactivated,
                CreatedAt = cashier.CreatedAt,
                UpdatedAt = cashier.UpdatedAt ?? DateTime.UtcNow
            });
        }

        [HttpDelete("{cashierId}")]
        public async Task<IActionResult> Delete(Guid cashierId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out var userId))
                return Unauthorized();

            var cashier = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == cashierId && u.OwnerId == userId && u.Role == "Cashier");

            if (cashier == null)
                return NotFound("Cashier not found or not owned by you.");

            _context.Users.Remove(cashier);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
