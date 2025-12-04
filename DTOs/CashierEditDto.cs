using Microsoft.AspNetCore.Mvc;

namespace ProjectK.API.DTOs
{
    public class CashierEditDto
    {
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public bool? IsDeactivated { get; set; }
    }
}
