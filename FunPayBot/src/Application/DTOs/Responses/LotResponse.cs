namespace FunPayBot.src.Application.DTOs.Responses
{
    public class LotResponse
    {
        public int Id { get; set; }
        public string? Server { get; set; }
        public string? Description { get; set; }
        public string? Title { get; set; }
        public int? Amount { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; }
        public int SellerId { get; set; }
        public string SellerUsername { get; set; }
        public bool AutoDelivery { get; set; }
        public bool? IsPromo { get; set; }
        public Dictionary<string, object>? Attributes { get; set; }
        public int SubcategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string Html { get; set; }
        public string PublicLink { get; set; }
    }
}
