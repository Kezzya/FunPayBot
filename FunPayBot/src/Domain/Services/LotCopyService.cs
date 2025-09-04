using FunPayBot.src.Application.DTOs.Requests;
using FunPayBot.src.Application.DTOs.Responses;
using FunPayBot.src.Domain.Entities;
using Microsoft.Extensions.Options;

namespace FunPayBot.src.Domain.Services
{
    public class LotCopyService
    {
        private readonly HttpClient _pythonApiClient;
        private readonly ILogger<LotCopyService> _logger;
        private readonly FunPaySettings _funPaySettings;

        public LotCopyService(
            IHttpClientFactory httpClientFactory,
            ILogger<LotCopyService> logger,
            IOptions<FunPaySettings> funPaySettings)
        {
            _pythonApiClient = httpClientFactory.CreateClient("PythonAPI");
            _logger = logger;
            _funPaySettings = funPaySettings.Value;
        }

        public async Task<LotResponse[]> CopyLotsByUserIdAsync(int userId, int subcategoryId)
        {
            _logger.LogInformation("Copying lots for user ID: {UserId}, subcategory: {SubcategoryId}", userId, subcategoryId);

            // Получить лоты пользователя
            string getLotsUrl = $"lots-by-user/{subcategoryId}/{userId}?golden_key={_funPaySettings.GoldenKey}";
            var getResponse = await _pythonApiClient.GetAsync(getLotsUrl);

            if (!getResponse.IsSuccessStatusCode)
            {
                var errorContent = await getResponse.Content.ReadAsStringAsync();
                _logger.LogError("FastAPI error while getting lots: {StatusCode} - {Error}", getResponse.StatusCode, errorContent);
                throw new Exception($"Failed to get lots: {errorContent}");
            }

            var userLots = await getResponse.Content.ReadFromJsonAsync<LotResponse[]>();
            if (userLots == null || !userLots.Any())
            {
                _logger.LogWarning("No lots found for user ID: {UserId}, subcategory: {SubcategoryId}", userId, subcategoryId);
                return [];
            }

            // Копировать лоты
            var createdLots = new List<LotResponse>();
            foreach (var lot in userLots)
            {
                var createLotRequest = new CreateLotRequest
                {
                    SubcategoryId = subcategoryId,
                    Price = lot.Price,
                    Description = lot.Description
                };

                var createResponse = await _pythonApiClient.PostAsJsonAsync($"create-lot?golden_key={_funPaySettings.GoldenKey}", createLotRequest);

                if (!createResponse.IsSuccessStatusCode)
                {
                    var errorContent = await createResponse.Content.ReadAsStringAsync();
                    _logger.LogError("FastAPI error while creating lot: {StatusCode} - {Error}", createResponse.StatusCode, errorContent);
                    continue;
                }

                var createdLot = await createResponse.Content.ReadFromJsonAsync<LotResponse>();
                if (createdLot != null)
                {
                    createdLots.Add(createdLot);
                }
            }

            _logger.LogInformation("Successfully copied {LotCount} lots for user ID: {UserId}", createdLots.Count, userId);
            return createdLots.ToArray();
        }
    }
}
