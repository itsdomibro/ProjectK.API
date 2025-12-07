namespace ProjectK.API.DTOs
{
    public class CategoryResponseDto
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; } 
        public string Description { get; set; } 
        public string Color { get; set; }
    }
}
