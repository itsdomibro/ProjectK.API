using System.ComponentModel.DataAnnotations;

namespace ProjectK.API.DTOs
{
    public class RegisterDto
    {
        [Required]
        [StringLength(64, MinimumLength = 4)]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(32, MinimumLength = 4)]
        public string Password { get; set; }

        [Required]
        [StringLength(32, MinimumLength = 2)]
        public string BusinessName { get; set; }

        public string? BusinessDescription { get; set; }
    }
}
