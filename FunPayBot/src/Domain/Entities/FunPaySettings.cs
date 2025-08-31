namespace FunPayBot.src.Domain.Entities
{
  
    public class FunPaySettings
    {
        public string GoldenKey { get; set; } = string.Empty;
        public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
        public string PythonApiUrl { get; set; } = "http://localhost:8000";
    }
}
