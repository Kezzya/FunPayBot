using FunPayBot.src.Domain.Interfaces;
using FunPayBot.src.Domain.Services;

namespace FunPayBot.src.Domain.Entities
{
    public class CopyLotsFeature : IFunPayBotFeature
    {
        private readonly CopyLotsService _lotCopyService;

        public string Name { get; set; } = "CopyLots";
        public string Description { get; set; } = "Скопировать чужие лоты";
        public bool IsActive { get;  set; } = true;
        public string ViewName { get; set; } = "CopyLotsView";

        public CopyLotsFeature(CopyLotsService lotCopyService)
        {
            _lotCopyService = lotCopyService;
        }

        public async Task ExecuteAsync(Dictionary<string, object> parameters = null)
        {
            if (!IsActive) return;

            await _lotCopyService.CopyLotsAsync(parameters);
        }
    }
}
