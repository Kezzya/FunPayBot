using FunPayBot.src.Application.DTOs.Responses;
using FunPayBot.src.Domain.Entities;
using FunPayBot.src.Domain.Services;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using System.Globalization;


namespace FunPayBot.src.Domain.Services
{
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

            // Вызов /auth для получения CsrfToken
            var authResponse = await _pythonApiClient.PostAsJsonAsync(
                $"/auth?golden_key={_funPaySettings.GoldenKey}",
                new { golden_key = _funPaySettings.GoldenKey }
            );
            if (!authResponse.IsSuccessStatusCode)
            {
                var errorContent = await authResponse.Content.ReadAsStringAsync();
                _logger.LogError("Failed to authenticate: {StatusCode} - {Error}", authResponse.StatusCode, errorContent);
                throw new Exception("Authentication failed");
            }

            var authData = await authResponse.Content.ReadFromJsonAsync<AuthResponse>();
            string csrfToken = authData.CsrfToken;

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
                    // Запрос полей для нового лота
                    string getFieldsUrl = $"lots/offerEdit?offer=0&node={lot.SubcategoryId}&golden_key={_funPaySettings.GoldenKey}";
                    _logger.LogInformation("Requesting fields for SubcategoryId: {SubcategoryId}, URL: {Url}", lot.SubcategoryId, getFieldsUrl);
                    var fieldsResponse = await _pythonApiClient.GetAsync(getFieldsUrl);
                    if (!fieldsResponse.IsSuccessStatusCode)
                    {
                        var errorContent = await fieldsResponse.Content.ReadAsStringAsync();
                        _logger.LogError("Failed to get lot fields: {StatusCode} - {Error}",
                            fieldsResponse.StatusCode, errorContent);

                        if (fieldsResponse.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
                        {
                            _logger.LogWarning("422 Error - likely invalid subcategory or insufficient permissions for SubcategoryId: {SubcategoryId}",
                                lot.SubcategoryId);
                            // Можно попробовать пропустить этот лот или использовать другую подкатегорию
                            continue;
                        }

                        throw new Exception($"Failed to get lot fields: {fieldsResponse.StatusCode} - {errorContent}");
                    }

                    // Десериализация HTML ответа
                    var htmlContent = await fieldsResponse.Content.ReadAsStringAsync();
                    var fields = ParseLotFieldsFromHtml(htmlContent);

                    // Заполнение полей
                    fields["csrf_token"] = csrfToken;
                    fields["offer_id"] = "0";
                    fields["node_id"] = lot.SubcategoryId.ToString();
                    fields["price"] = lot.Price.ToString();
                    fields["fields[summary][ru]"] = lot.Title ?? "";
                    fields["fields[summary][en]"] = lot.Title ?? "";
                    fields["fields[desc][ru]"] = lot.Description ?? "";
                    fields["fields[desc][en]"] = lot.DescriptionEn ?? "";
                    fields["param_0"] = lot.Server ?? "";
                    fields["amount"] = lot.Amount?.ToString() ?? "";
                    fields["auto_delivery"] = lot.AutoDelivery ? "on" : "";
                    fields["fields[attributes]"] = lot.Attributes != null ? string.Join(",", lot.Attributes.Select(kv => $"{kv.Key}:{kv.Value}")) : "";

                    // Создание лота
                
                    var formContent = new FormUrlEncodedContent(fields.Select(kvp =>
                        new KeyValuePair<string, string>(kvp.Key, kvp.Value?.ToString() ?? "")));

                    var createResponse = await _pythonApiClient.PostAsync(
                        $"create-lot-from-fields?golden_key={_funPaySettings.GoldenKey}",
                        formContent
                    );
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


        public class AuthResponse
        {
            public string Username { get; set; }
            public int Id { get; set; }
            public string CsrfToken { get; set; }
        }
        // Метод для парсинга HTML и извлечения полей
        private Dictionary<string, object> ParseLotFieldsFromHtml(string htmlContent)
        {
            var fields = new Dictionary<string, object>();
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            // Парсинг input полей
            var inputs = doc.DocumentNode.SelectNodes("//input");
            if (inputs != null)
            {
                foreach (var input in inputs)
                {
                    var name = input.GetAttributeValue("name", null);
                    if (name != null)
                    {
                        fields[name] = input.GetAttributeValue("value", "");
                    }
                }
            }

            // Парсинг textarea
            var textareas = doc.DocumentNode.SelectNodes("//textarea");
            if (textareas != null)
            {
                foreach (var textarea in textareas)
                {
                    var name = textarea.GetAttributeValue("name", null);
                    if (name != null)
                    {
                        fields[name] = textarea.InnerText.Trim();
                    }
                }
            }

            // Парсинг select
            var selects = doc.DocumentNode.SelectNodes("//select");
            if (selects != null)
            {
                foreach (var select in selects)
                {
                    var name = select.GetAttributeValue("name", null);
                    if (name != null)
                    {
                        var parent = select.ParentNode;
                        if (parent != null && !parent.GetClasses().Contains("hidden"))
                        {
                            var selectedOption = select.SelectSingleNode(".//option[@selected]");
                            if (selectedOption != null)
                            {
                                fields[name] = selectedOption.GetAttributeValue("value", "");
                            }
                        }
                    }
                }
            }

            // Парсинг checkbox
            var checkboxes = doc.DocumentNode.SelectNodes("//input[@type='checkbox']");
            if (checkboxes != null)
            {
                foreach (var checkbox in checkboxes)
                {
                    var name = checkbox.GetAttributeValue("name", null);
                    if (name != null && checkbox.GetAttributeValue("checked", null) != null)
                    {
                        fields[name] = "on";
                    }
                }
            }

            return fields;
        }
    }
}