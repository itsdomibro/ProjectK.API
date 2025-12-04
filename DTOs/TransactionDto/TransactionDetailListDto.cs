namespace ProjectK.API.DTOs.TransactionDto
{
    public class TransactionDetailListDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public long Price { get; set; }
        public long Subtotal => Quantity * Price;
    }

}
