namespace ProjectK.API.DTOs.TransactionDto
{
    public class TransactionDetailListDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public decimal Subtotal => Quantity * (Price - Discount);
    }

}
