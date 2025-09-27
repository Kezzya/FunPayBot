using FunPayBot.src.Application.DTOs.Responses;
using FunPayBot.src.Domain.Entities;
using FunPayBot.src.Domain.Services;
using Microsoft.Extensions.Options;
using System.Globalization;

public class CopyLotsService
{
    private readonly HttpClient _pythonApiClient;
    private readonly ILogger<CopyLotsService> _logger;
    private readonly FunPaySettings _funPaySettings;
    private readonly AuthService _authService;

    public CopyLotsService(
        IHttpClientFactory httpClientFactory,
        ILogger<CopyLotsService> logger,
        IOptions<FunPaySettings> funPaySettings,
        AuthService authService)
    {
        _pythonApiClient = httpClientFactory.CreateClient("PythonAPI");
        _logger = logger;
        _funPaySettings = funPaySettings.Value;
        _authService = authService;
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

        // Аутентификация
        var authResponse = await _authService.AuthenticateAsync();
        if (authResponse == null || string.IsNullOrEmpty(authResponse.CsrfToken))
        {
            _logger.LogError("Authentication failed or CSRF token missing");
            throw new Exception("Authentication failed or CSRF token missing");
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
                newFields["csrf_token"] = authResponse.CsrfToken;
                newFields["offer_id"] = "0";
                newFields["node_id"] = lot.SubcategoryId.ToString();
                newFields["price"] = lot.Price.ToString(CultureInfo.InvariantCulture);
                newFields["fields[summary][ru]"] = lot.Title ?? "";
                newFields["fields[summary][en]"] = lot.Title ?? "";
                newFields["fields[desc][ru]"] = lot.Description ?? "";
                newFields["fields[desc][en]"] = lot.Description ?? "";
                newFields["param_0"] = lot.Server ?? "";
                newFields["amount"] = lot.Amount?.ToString() ?? "";
                newFields["auto_delivery"] = lot.AutoDelivery ? "on" : null;
                newFields["fields[attributes]"] = lot.Attributes != null
                    ? string.Join(",", lot.Attributes.Select(kv => $"{kv.Key}:{kv.Value}"))
                    : "";

                // Обработка изображений
                var photoIds = new List<string>();
                if (!string.IsNullOrEmpty(lot.PublicLink))
                {
                    string lotDetailsUrl = $"lot-details/{lot.Id}?golden_key={_funPaySettings.GoldenKey}";
                    var lotDetailsResponse = await _pythonApiClient.GetAsync(lotDetailsUrl);
                    if (!lotDetailsResponse.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Failed to get lot details for lot {LotId}", lot.Id);
                        continue;
                    }
                    var lotPage = await lotDetailsResponse.Content.ReadFromJsonAsync<LotResponse>();
                }

                // Создать лот
                var createResponse = await _pythonApiClient.PostAsJsonAsync(
                    $"create-lot?golden_key={_funPaySettings.GoldenKey}", newFields);
                if (!createResponse.IsSuccessStatusCode)
                {
                    var errorContent = await createResponse.Content.ReadAsStringAsync();
                    _logger.LogError("FastAPI error while creating lot: {StatusCode} - {Error}",
                        createResponse.StatusCode, errorContent);
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