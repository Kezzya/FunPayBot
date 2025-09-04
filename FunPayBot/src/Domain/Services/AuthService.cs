using FunPayBot.src.Application.DTOs.Requests;
using FunPayBot.src.Application.DTOs.Responses;
using FunPayBot.src.Domain.Entities;
using Microsoft.Extensions.Options;

namespace FunPayBot.src.Domain.Services
{
    public class AuthService
    {
        private readonly HttpClient _pythonApiClient;
        private readonly ILogger<AuthService> _logger;
        private readonly FunPaySettings _funPaySettings;

        public AuthService(IHttpClientFactory httpClientFactory, ILogger<AuthService> logger, IOptions<FunPaySettings> funPaySettings)
        {
            _pythonApiClient = httpClientFactory.CreateClient("PythonAPI");
            _logger = logger;
            _funPaySettings = funPaySettings.Value;
        }

        public async Task<AuthResponse> AuthenticateAsync()
        {
            var request = new AuthRequest
            {
                golden_key = _funPaySettings.GoldenKey,
                user_agent = _funPaySettings.UserAgent
            };

            var response = await _pythonApiClient.PostAsJsonAsync("auth", request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("FastAPI error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new Exception($"Authentication failed: {errorContent}");
            }

            return await response.Content.ReadFromJsonAsync<AuthResponse>();
        }
    }
}
