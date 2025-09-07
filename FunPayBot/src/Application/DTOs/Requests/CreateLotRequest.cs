namespace FunPayBot.src.Application.DTOs.Requests
{
    public class CreateLotRequest
    {
        public int SubcategoryId { get; set; }
        public decimal Price { get; set; }
        public string ShortDescription { get; set; }
        public string DetailedDescription { get; set; }
        public string Server { get; set; } // param_0
        public int? Quantity { get; set; }
        public bool AutoDelivery { get; set; }
    }
}
