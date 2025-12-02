using ProjectK.API.Models;

namespace ProjectK.API.DTOs
{
    public class CreateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public Guid? CategoryId { get; set; }
        public string? ImageUrl { get; set; }
    }
}
