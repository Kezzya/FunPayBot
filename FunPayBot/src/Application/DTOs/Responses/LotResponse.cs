namespace FunPayBot.src.Application.DTOs.Responses
{
    public class LotResponse
    {
        public int Id { get; set; }
        public float Price { get; set; }
        public string Description { get; set; }
        public int SellerId { get; set; }
        public string SellerUsername { get; set; }
    }
}
