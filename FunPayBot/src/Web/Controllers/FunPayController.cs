using FunPayBot.src.Application.DTOs.Requests;
using FunPayBot.src.Application.DTOs.Responses;
using FunPayBot.src.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FunPayBot.src.Web.Controllers
{
    
    [Route("api")]
    [ApiController]
    public class FunPayController : ControllerBase
    {
        private readonly HttpClient _regularHttpClient;
        private readonly HttpClient _pythonApiClient;
        private readonly ILogger<FunPayController> _logger;
        private readonly FunPaySettings _funPaySettings;

        public FunPayController(
            IHttpClientFactory httpClientFactory,
            HttpClient regularHttpClient,
            ILogger<FunPayController> logger,
            IOptions<FunPaySettings> funPaySettings) // Use IOptions
        {
            _regularHttpClient = regularHttpClient;
            _pythonApiClient = httpClientFactory.CreateClient("PythonAPI");
            _logger = logger;
            _funPaySettings = funPaySettings.Value;  
        }

        [HttpPost("auth")]
        public async Task<IActionResult> Authenticate()
        {
            _logger.LogInformation("Starting authentication...");

            try
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
                    _logger.LogError("FastAPI error: {StatusCode} - {Error}",
                                   response.StatusCode, errorContent);
                    return BadRequest($"Authentication failed: {errorContent}");
                }

                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                _logger.LogInformation("Authentication successful");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("lots/{subcategoryId}")]
        public async Task<IActionResult> GetLots(int subcategoryId)
        {
            string goldenKey = _funPaySettings.GoldenKey;
            _logger.LogInformation("Getting lots for subcategory: {SubcategoryId}, goldenKey: {GoldenKey}",
                                  subcategoryId, goldenKey?.Substring(0, Math.Min(8, goldenKey?.Length ?? 0)) + "***");

            try
            {
                var requestUrl = $"lots/{subcategoryId}?golden_key={goldenKey}";
                _logger.LogInformation("Sending request to Python API: {RequestUrl}", requestUrl);

                var response = await _pythonApiClient.GetAsync(requestUrl);

                _logger.LogInformation("Python API response: {StatusCode}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Python API error: {StatusCode} - {ErrorContent}",
                                   response.StatusCode, errorContent);
                    return BadRequest($"Python API error: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Python API response content length: {ContentLength}", responseContent.Length);
                _logger.LogDebug("Python API response content: {ResponseContent}", responseContent);

                // Парсим JSON
                var result = await response.Content.ReadFromJsonAsync<LotResponse[]>();
                _logger.LogInformation("Successfully parsed {LotCount} lots", result?.Length ?? 0);

                return Ok(result);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while getting lots for subcategory {SubcategoryId}", subcategoryId);
                return BadRequest($"Network error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error while getting lots for subcategory {SubcategoryId}", subcategoryId);
                return BadRequest($"Invalid JSON response: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while getting lots for subcategory {SubcategoryId}", subcategoryId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("get-lots-by-userid")]
        public async Task<IActionResult> GetLotsByUserId([FromBody] GetLotsByUserIdRequest request)
        {
            _logger.LogInformation("Getting lots for user ID: {UserId}, subcategory: {SubcategoryId}",
                request.UserId, request.SubcategoryId);

            try
            {
                string requestUrl = $"lots-by-user/{request.SubcategoryId}/{request.UserId}?golden_key={_funPaySettings.GoldenKey}";
                var response = await _pythonApiClient.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("FastAPI error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    return BadRequest($"Failed to get lots: {errorContent}");
                }

                var userLots = await response.Content.ReadFromJsonAsync<LotResponse[]>();
                if (userLots == null || !userLots.Any())
                {
                    _logger.LogWarning("No lots returned for user ID: {UserId}, subcategory: {SubcategoryId}",
                        request.UserId, request.SubcategoryId);
                    return NotFound("No lots found for this user in the specified subcategory");
                }

                _logger.LogInformation("Found {LotCount} lots for user ID: {UserId}", userLots.Length, request.UserId);
                return Ok(userLots);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while getting lots for user ID {UserId}", request.UserId);
                return BadRequest($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while getting lots for user ID {UserId}", request.UserId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("copy-lots-by-userid")]
        public async Task<IActionResult> CopyLotsByUserId([FromBody] CopyLotsByUserIdRequest request)
        {
            _logger.LogInformation("Copying lots for user ID: {UserId}, subcategory: {SubcategoryId}",
                request.UserId, request.SubcategoryId);

            try
            {
                // Получить лоты пользователя
                string getLotsUrl = $"lots-by-user/{request.SubcategoryId}/{request.UserId}?golden_key={_funPaySettings.GoldenKey}";
                var getResponse = await _pythonApiClient.GetAsync(getLotsUrl);

                if (!getResponse.IsSuccessStatusCode)
                {
                    var errorContent = await getResponse.Content.ReadAsStringAsync();
                    _logger.LogError("FastAPI error while getting lots: {StatusCode} - {Error}", getResponse.StatusCode, errorContent);
                    return BadRequest($"Failed to get lots: {errorContent}");
                }

                var userLots = await getResponse.Content.ReadFromJsonAsync<LotResponse[]>();
                if (userLots == null || !userLots.Any())
                {
                    _logger.LogWarning("No lots found for user ID: {UserId}, subcategory: {SubcategoryId}",
                        request.UserId, request.SubcategoryId);
                    return NotFound("No lots found to copy for this user in the specified subcategory");
                }

                // Копировать лоты
                var createdLots = new List<LotResponse>();
                foreach (var lot in userLots)
                {
                    var createLotRequest = new CreateLotRequest
                    {
                        SubcategoryId = request.SubcategoryId,
                        Price = lot.Price,
                        Description = lot.Description
                    };

                    var createResponse = await _pythonApiClient.PostAsJsonAsync($"create-lot?golden_key={_funPaySettings.GoldenKey}", createLotRequest);

                    if (!createResponse.IsSuccessStatusCode)
                    {
                        var errorContent = await createResponse.Content.ReadAsStringAsync();
                        _logger.LogError("FastAPI error while creating lot: {StatusCode} - {Error}", createResponse.StatusCode, errorContent);
                        continue; // Пропустить ошибочные лоты
                    }

                    var createdLot = await createResponse.Content.ReadFromJsonAsync<LotResponse>();
                    if (createdLot != null)
                    {
                        createdLots.Add(createdLot);
                    }
                }

                if (!createdLots.Any())
                {
                    _logger.LogWarning("No lots were successfully copied for user ID: {UserId}", request.UserId);
                    return BadRequest("No lots were successfully copied");
                }

                _logger.LogInformation("Successfully copied {LotCount} lots for user ID: {UserId}", createdLots.Count, request.UserId);
                return Ok(createdLots);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while copying lots for user ID {UserId}", request.UserId);
                return BadRequest($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while copying lots for user ID {UserId}", request.UserId);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}





 




