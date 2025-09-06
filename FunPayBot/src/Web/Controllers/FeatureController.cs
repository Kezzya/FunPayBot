using FunPayBot.src.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FunPayBot.src.Web.Controllers
{
    public class FeatureController : Controller

    {
        private readonly IEnumerable<IFunPayBotFeature> _features;
        private readonly ILogger<FunPayController> _logger;


        public FeatureController(IEnumerable<IFunPayBotFeature> features, ILogger<FunPayController> logger)
        {
            _features = features;
            _logger = logger;
        }

        [HttpGet("feature/{featureName}")]
        public IActionResult Show(string featureName)
        {
            var feature = _features.FirstOrDefault(f =>
                f.Name.Equals(featureName, StringComparison.OrdinalIgnoreCase));

            if (feature == null || !feature.IsActive)
                return NotFound();

            // Возвращаем представление для конкретной фичи
            return View(feature.ViewName, feature);

        }


    [HttpPost("feature/{featureName}/execute")]
        public async Task<IActionResult> Execute(string featureName)
        {
            var feature = _features.FirstOrDefault(f =>
                f.Name.Equals(featureName, StringComparison.OrdinalIgnoreCase));
            if (feature == null)
                return NotFound();
            try
            {
                await feature.ExecuteAsync();
                return Ok(new { message = $"{feature.Name} executed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing feature {FeatureName}", featureName);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
