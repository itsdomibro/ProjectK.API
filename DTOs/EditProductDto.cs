namespace ProjectK.API.DTOs
{
    public class EditProductDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public decimal? Discount { get; set; }
        public Guid? CategoryId { get; set; }
        public string? ImageUrl { get; set; }
    }
}
