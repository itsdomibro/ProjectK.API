namespace ProjectK.API.DTOs.TransactionDto
{
    public class CreateTransactionDto
    {
        public string Payment { get; set; }
        public List<TransactionItemDto> Items { get; set; } = new List<TransactionItemDto>();
    }
}
