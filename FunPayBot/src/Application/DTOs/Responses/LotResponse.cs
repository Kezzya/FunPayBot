using System.Text.Json.Serialization;

namespace FunPayBot.src.Application.DTOs.Responses
{
    public class LotResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("server")]
        public string? Server { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("DescriptionEn")]
        public string? DescriptionEn { get; set; }  // Новое поле

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("amount")]
        public int? Amount { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("sellerId")]
        public int SellerId { get; set; }

        [JsonPropertyName("sellerUsername")]
        public string SellerUsername { get; set; }

        [JsonPropertyName("autoDelivery")]
        public bool AutoDelivery { get; set; }

        [JsonPropertyName("isPromo")]
        public bool? IsPromo { get; set; }

        [JsonPropertyName("attributes")]
        public Dictionary<string, object>? Attributes { get; set; }

        [JsonPropertyName("subcategoryId")]
        public int SubcategoryId { get; set; }

        [JsonPropertyName("categoryName")]
        public string? CategoryName { get; set; }

        [JsonPropertyName("html")]
        public string Html { get; set; }

        [JsonPropertyName("publicLink")]
        public string PublicLink { get; set; }
    }

}
