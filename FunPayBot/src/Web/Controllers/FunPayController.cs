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
        public async Task<IActionResult> GetLots(int subcategoryId, [FromQuery] string goldenKey)
        {
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

    }

    public class AuthRequest
    {
        public string golden_key { get; set; }
        public string user_agent { get; set; } = "Mozilla/5.0";
    }

    public class AuthResponse
    {
        public string Username { get; set; }
        public int Id { get; set; }
    }

    public class LotResponse
    {
        public int Id { get; set; }
        public float Price { get; set; }
        public string Description { get; set; }
    }
}
