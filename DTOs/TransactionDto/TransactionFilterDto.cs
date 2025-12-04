namespace ProjectK.API.DTOs.TransactionDto
{
    public class TransactionFilterDto
    {
        public string? Search { get; set; }
        public bool? IsPaid { get; set; }
        public string? Payment { get; set; }

        public string? SortBy { get; set; } // "date", "amount"
        public string? SortOrder { get; set; } // "asc" / "desc"
    }

}
