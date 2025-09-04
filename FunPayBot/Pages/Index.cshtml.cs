using FunPayBot.src.Domain.Entities;
using FunPayBot.src.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FunPayBot.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IEnumerable<IFunPayBotFeature> _botFeatures;
        public IndexModel(ILogger<IndexModel> logger, IEnumerable<IFunPayBotFeature> botFeatures)
        {
            _logger = logger;
            _botFeatures = botFeatures;
        }
        public List<IFunPayBotFeature> BotFeatures { get; set; }  
        public void OnGet()
        {
            BotFeatures = _botFeatures.Any() ? _botFeatures.ToList() : new List<IFunPayBotFeature>();
        }
    }
}
