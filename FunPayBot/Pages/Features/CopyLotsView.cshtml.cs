using FunPayBot.src.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FunPayBot.Pages.CopyLots
{
    public class CopyLotsViewModel : PageModel
    {
        private readonly IEnumerable<IFunPayBotFeature> _features;

        public CopyLotsViewModel(IEnumerable<IFunPayBotFeature> features)
        {
            _features = features;
        }

        public IFunPayBotFeature Feature { get; set; }

        public IActionResult OnGet(string featureName)
        {
            Feature = _features.FirstOrDefault(f =>
                f.Name.Equals(featureName, StringComparison.OrdinalIgnoreCase));

            if (Feature == null || !Feature.IsActive)
                return NotFound();

            return Page();
        }
    }
}
