using FunPayBot.src.Application.DTOs.Requests;
using FunPayBot.src.Application.DTOs.Responses;
using FunPayBot.src.Domain.Entities;
using FunPayBot.src.Domain.Services;
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
        private readonly AuthService _authService;
        private readonly LotFetchService _lotFetchService;
        private readonly LotCopyService _lotCopyService;
        private readonly ILogger<FunPayController> _logger;

        public FunPayController(
            AuthService authService,
            LotFetchService lotFetchService,
            LotCopyService lotCopyService,
            ILogger<FunPayController> logger)
        {
            _authService = authService;
            _lotFetchService = lotFetchService;
            _lotCopyService = lotCopyService;
            _logger = logger;
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

        [HttpPost("copy-lots-by-userid")]
        public async Task<IActionResult> CopyLotsByUserId([FromBody] CopyLotsByUserIdRequest request)
        {
            try
            {
                var createdLots = await _lotCopyService.CopyLotsByUserIdAsync(request.UserId, request.SubcategoryId);
                if (!createdLots.Any())
                {
                    _logger.LogWarning("No lots were successfully copied for user ID: {UserId}", request.UserId);
                    return BadRequest("No lots were successfully copied");
                }
                _logger.LogInformation("Successfully copied {LotCount} lots for user ID: {UserId}", createdLots.Length, request.UserId);
                return Ok(createdLots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while copying lots for user ID {UserId}", request.UserId);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}





 




