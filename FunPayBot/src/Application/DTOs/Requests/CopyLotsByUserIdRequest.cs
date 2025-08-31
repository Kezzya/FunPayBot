namespace FunPayBot.src.Application.DTOs.Requests
{
    public class GetLotsByUserIdRequest
    {
        public int UserId { get; set; }
        public int SubcategoryId { get; set; }
    }
}
