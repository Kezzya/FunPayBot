namespace FunPayBot.src.Application.DTOs.Responses
{
    public class AuthResponse
    {
        public string Username { get; set; }
        public int Id { get; set; }
        public string CsrfToken { get; set; }
    }
}
