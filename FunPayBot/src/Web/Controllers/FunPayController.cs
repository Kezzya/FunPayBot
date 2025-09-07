using FunPayBot.src.Application.DTOs.Requests;
using FunPayBot.src.Application.DTOs.Responses;
using FunPayBot.src.Domain.Entities;
using FunPayBot.src.Domain.Interfaces;
using FunPayBot.src.Domain.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FunPayBot.src.Web.Controllers
{
    [Route("api")]
    [ApiController]
    public class FunPayController : Controller
    {
        private readonly AuthService _authService;
        private readonly LotFetchService _lotFetchService;
        private readonly ILogger<FunPayController> _logger;
        private readonly IEnumerable<IFunPayBotFeature> _features;

        public FunPayController(
    AuthService authService,
    LotFetchService lotFetchService,
    ILogger<FunPayController> logger,
    IEnumerable<IFunPayBotFeature> features)
        {
            _authService = authService;
            _lotFetchService = lotFetchService;
            _logger = logger;
            _features = features;
        }

        [HttpPost("auth")]
        public async Task<IActionResult> Authenticate()
        {
            try
            {
                var result = await _authService.AuthenticateAsync();
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
            try
            {
                var result = await _lotFetchService.GetLotsAsync(subcategoryId);
                _logger.LogInformation("Successfully parsed {LotCount} lots", result?.Length ?? 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting lots for subcategory {SubcategoryId}", subcategoryId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("get-lots-by-userid")]
        public async Task<IActionResult> GetLotsByUserId([FromBody] GetLotsByUserIdRequest request)
        {
            try
            {
                var userLots = await _lotFetchService.GetLotsByUserIdAsync(request.UserId, request.SubcategoryId);
                if (userLots == null || !userLots.Any())
                {
                    _logger.LogWarning("No lots found for user ID: {UserId}, subcategory: {SubcategoryId}", request.UserId, request.SubcategoryId);
                    return NotFound("No lots found for this user in the specified subcategory");
                }
                _logger.LogInformation("Found {LotCount} lots for user ID: {UserId}", userLots.Length, request.UserId);
                return Ok(userLots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting lots for user ID {UserId}", request.UserId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("user/{userId}/info")]
        public async Task<IActionResult> GetUserInfo(int userId)
        {
            using var client = new HttpClient();
            var url = $"https://funpay.com/users/{userId}/";
            var html = await client.GetStringAsync(url);

            // Regex to extract username from <h1 class="mb40 ..."><span class="mr4">Username</span>
            var usernameMatch = Regex.Match(html, @"<h1[^>]*class\s*=\s*[""']mb40[^""']*[""'][^>]*>\s*<span[^>]*class\s*=\s*[""']mr4[""'][^>]*>([^<]+)</span>");

            // Regex to extract background-image url
            var avatarMatch = Regex.Match(html, @"<div class=""avatar-photo"" style=""background-image: url\(([^)]+)\);""");

            if (usernameMatch.Success && avatarMatch.Success)
            {
                var username = usernameMatch.Groups[1].Value.Trim();
                var imageUrl = avatarMatch.Groups[1].Value;

                return Ok(new { username, imageUrl });
            }

            return NotFound();
        }
        [HttpPost]
        public async Task<IActionResult> ExecuteFeature(string featureName)
        {
            var feature = _features.FirstOrDefault(f =>
                f.Name.Equals(featureName, StringComparison.OrdinalIgnoreCase));

            if (feature == null || !feature.IsActive)
                return NotFound($"Feature '{featureName}' not found or inactive");

            try
            {
                await feature.ExecuteAsync();
                return Ok($"Feature '{featureName}' executed successfully");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error executing feature: {ex.Message}");
            }
        }
    }
}





 




