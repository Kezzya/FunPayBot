using FunPayBot.src.Application.DTOs.Responses;
using FunPayBot.src.Domain.Entities;
using Microsoft.Extensions.Options;

namespace FunPayBot.src.Domain.Services
{
    public class LotFetchService
    {
        private readonly HttpClient _pythonApiClient;
        private readonly ILogger<LotFetchService> _logger;
        private readonly FunPaySettings _funPaySettings;

        public LotFetchService(IHttpClientFactory httpClientFactory, ILogger<LotFetchService> logger, IOptions<FunPaySettings> funPaySettings)
        {
            _pythonApiClient = httpClientFactory.CreateClient("PythonAPI");
            _logger = logger;
            _funPaySettings = funPaySettings.Value;
        }

        public async Task<LotResponse[]> GetLotsAsync(int subcategoryId)
        {
            var requestUrl = $"lots/{subcategoryId}?golden_key={_funPaySettings.GoldenKey}";
            var response = await _pythonApiClient.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Python API error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                throw new Exception($"Python API error: {response.StatusCode} - {errorContent}");
            }

            return await response.Content.ReadFromJsonAsync<LotResponse[]>();
        }

        public async Task<LotResponse[]> GetLotsByUserIdAsync(int userId, int subcategoryId)
        {
            var requestUrl = $"lots-by-user/{subcategoryId}/{userId}?golden_key={_funPaySettings.GoldenKey}";
            var response = await _pythonApiClient.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("FastAPI error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new Exception($"Failed to get lots: {errorContent}");
            }

            return await response.Content.ReadFromJsonAsync<LotResponse[]>();
        }
    }
}
