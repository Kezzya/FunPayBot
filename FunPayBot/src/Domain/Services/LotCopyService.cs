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

        public async Task<LotResponse[]> CopyLotsByUserIdAsync(int userId, int? subcategoryId)
        {
            _logger.LogInformation("Copying lots for user ID: {UserId}, subcategory: {SubcategoryId}", userId, subcategoryId);

            List<int> subcategoriesToProcess;

            if (subcategoryId == null || subcategoryId == -1)
            {
                subcategoriesToProcess = await GetUserSubcategoriesAsync(userId);
                _logger.LogInformation("Found {Count} subcategories for user {UserId}", subcategoriesToProcess.Count, userId);
            }
            else
            {
                subcategoriesToProcess = new List<int> { subcategoryId.Value };
            }

            var allCreatedLots = new List<LotResponse>();

            foreach (var subcat in subcategoriesToProcess)
            {
                try
                {
                    var lotsFromSubcategory = await CopyLotsFromSubcategoryAsync(userId, subcat);
                    allCreatedLots.AddRange(lotsFromSubcategory);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error copying lots from subcategory {SubcategoryId} for user {UserId}", subcat, userId);
                }
            }

            _logger.LogInformation("Successfully copied {LotCount} lots total for user ID: {UserId}", allCreatedLots.Count, userId);
            return allCreatedLots.ToArray();
        }

        private async Task<List<int>> GetUserSubcategoriesAsync(int userId)
        {
            string getSubcategoriesUrl = $"user-subcategories/{userId}?golden_key={_funPaySettings.GoldenKey}";
            var response = await _pythonApiClient.GetAsync(getSubcategoriesUrl);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("FastAPI error while getting user subcategories: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new Exception($"Failed to get user subcategories: {errorContent}");
            }

            var subcategories = await response.Content.ReadFromJsonAsync<int[]>();
            return subcategories?.ToList() ?? new List<int>();
        }

        private async Task<List<LotResponse>> CopyLotsFromSubcategoryAsync(int userId, int subcategoryId)
        {
            _logger.LogInformation("Processing subcategory {SubcategoryId} for user {UserId}", subcategoryId, userId);

            string getLotsUrl = $"lots-by-user/{subcategoryId}/{userId}?golden_key={_funPaySettings.GoldenKey}";
            var getResponse = await _pythonApiClient.GetAsync(getLotsUrl);

            if (!getResponse.IsSuccessStatusCode)
            {
                var errorContent = await getResponse.Content.ReadAsStringAsync();
                _logger.LogError("FastAPI error while getting lots: {StatusCode} - {Error}", getResponse.StatusCode, errorContent);
                throw new Exception($"Failed to get lots for subcategory {subcategoryId}: {errorContent}");
            }

            var userLots = await getResponse.Content.ReadFromJsonAsync<LotResponse[]>();
            if (userLots == null || !userLots.Any())
            {
                _logger.LogInformation("No lots found for user ID: {UserId}, subcategory: {SubcategoryId}", userId, subcategoryId);
                return new List<LotResponse>();
            }

            var createdLots = new List<LotResponse>();
            foreach (var lot in userLots)
            {
                // Получить детали лота (detailed_description, images)
                string getLotDetailsUrl = $"lot-details/{lot.Id}?golden_key={_funPaySettings.GoldenKey}";
                var detailsResponse = await _pythonApiClient.GetAsync(getLotDetailsUrl);
                if (!detailsResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get lot details for lot {LotId}", lot.Id);
                    continue;
                }
                var lotDetails = await detailsResponse.Content.ReadFromJsonAsync<LotResponse>();
                if (lotDetails != null)
                {
                    lot.DetailedDescription = lotDetails.DetailedDescription;
                }

                // Получить пустые поля для нового лота
                string getFieldsUrl = $"lot-fields/new/{subcategoryId}?golden_key={_funPaySettings.GoldenKey}";
                var fieldsResponse = await _pythonApiClient.GetAsync(getFieldsUrl);
                if (!fieldsResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get new lot fields for subcategory {SubcategoryId}", subcategoryId);
                    continue;
                }
                var newFields = await fieldsResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>(); // LotFields-like

                // Заполнить поля
                newFields["price"] = lot.Price;
                newFields["short_description"] = lot.Description;
                newFields["description"] = lot.DetailedDescription;
                newFields["param_0"] = lot.Server; // Сервер
                newFields["quantity"] = lot.Amount;
                newFields["auto_delivery"] = lot.AutoDelivery ? "on" : null;

                // Создать лот
                var createResponse = await _pythonApiClient.PostAsJsonAsync($"create-lot?golden_key={_funPaySettings.GoldenKey}", newFields);
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

            _logger.LogInformation("Copied {LotCount} lots from subcategory {SubcategoryId}", createdLots.Count, subcategoryId);
            return createdLots;
        }
    }
}
