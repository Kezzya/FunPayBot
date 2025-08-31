namespace FunPayBot.src.Application.DTOs.Requests
{
    public class AuthRequest
    {
        public string golden_key { get; set; }
        public string user_agent { get; set; } = "Mozilla/5.0";
    }
}
