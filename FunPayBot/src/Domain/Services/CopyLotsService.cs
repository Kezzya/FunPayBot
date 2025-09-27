using FunPayBot.src.Application.DTOs.Requests;
using FunPayBot.src.Application.DTOs.Responses;
using FunPayBot.src.Domain.Entities;
using Microsoft.Extensions.Options;

namespace FunPayBot.src.Domain.Services
{
    public class CopyLotsService
    {
        private readonly HttpClient _pythonApiClient;
        private readonly ILogger<CopyLotsService> _logger;
        private readonly FunPaySettings _funPaySettings;

        public CopyLotsService(
            IHttpClientFactory httpClientFactory,
            ILogger<CopyLotsService> logger,
            IOptions<FunPaySettings> funPaySettings)
        {
            _pythonApiClient = httpClientFactory.CreateClient("PythonAPI");
            _logger = logger;
            _funPaySettings = funPaySettings.Value;
        }

        public async Task<LotResponse[]> CopyLotsAsync(Dictionary<string, object> parameters)
        {
            _logger.LogInformation("Processing lots from parameters");

            if (!parameters.TryGetValue("lots", out var lotsObj))
            {
                _logger.LogError("Lots not provided in parameters");
                throw new ArgumentException("Lots not provided");
            }

            var userLots = System.Text.Json.JsonSerializer.Deserialize<LotResponse[]>(lotsObj.ToString());
            if (userLots == null || !userLots.Any())
            {
                _logger.LogInformation("No lots provided in parameters");
                return Array.Empty<LotResponse>();
            }

            var createdLots = new List<LotResponse>();

            foreach (var lot in userLots)
            {
                try
                {
                    // Получить пустые поля для нового лота
                    string getFieldsUrl = $"lot-fields/new/{lot.SubcategoryId}?golden_key={_funPaySettings.GoldenKey}";
                    var fieldsResponse = await _pythonApiClient.GetAsync(getFieldsUrl);
                    if (!fieldsResponse.IsSuccessStatusCode)
                    {
                        _logger.LogError("Failed to get new lot fields for subcategory {SubcategoryId}", lot.SubcategoryId);
                        continue;
                    }
                    var newFields = await fieldsResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();

                    // Заполнить поля
                    newFields["price"] = lot.Price;
                    newFields["short_description"] = lot.Title;
                    newFields["description"] = lot.Description;
                    newFields["param_0"] = lot.Server;
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
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error copying lot ID: {LotId}", lot.Id);
                }
            }

            _logger.LogInformation("Successfully copied {LotCount} lots", createdLots.Count);
            return createdLots.ToArray();
        }

    }
}
