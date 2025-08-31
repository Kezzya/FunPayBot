namespace FunPayBot.src.Application.DTOs.Requests
{
    public class CreateLotRequest
    {
        public int SubcategoryId { get; set; }
        public float Price { get; set; }
        public string Description { get; set; }
    }
}
