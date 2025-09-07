namespace FunPayBot.src.Application.DTOs.Responses
{
    public class LotResponse
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string DetailedDescription { get; set; }
        public int SellerId { get; set; }
        public string Server { get; set; }
        public int? Amount { get; set; }
        public bool AutoDelivery { get; set; }
        public string SellerUsername { get; set; }
    }
}
