using System.ComponentModel.DataAnnotations;

namespace ProjectK.API.DTOs
{
    public class CashierCreateDto
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
