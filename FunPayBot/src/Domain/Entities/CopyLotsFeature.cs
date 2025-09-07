using FunPayBot.src.Domain.Interfaces;
using FunPayBot.src.Domain.Services;

namespace FunPayBot.src.Domain.Entities
{
    public class CopyLotsFeature : IFunPayBotFeature
    {
        private readonly LotCopyService _lotCopyService;

        public string Name { get; set; } = "CopyLots";
        public string Description { get; set; } = "Скопировать чужие лоты";
        public bool IsActive { get;  set; } = true;
        public string ViewName { get; set; } = "CopyLotsView";

        public CopyLotsFeature(LotCopyService lotCopyService)
        {
            _lotCopyService = lotCopyService;
        }

        public async Task ExecuteAsync(Dictionary<string, object> parameters = null)
        {
            if (!IsActive) return;

            var userId = (int)parameters["userId"];
            var subcategoryId = parameters.ContainsKey("subcategoryId") ? (int)parameters["subcategoryId"] : -1;

            await _lotCopyService.CopyLotsByUserIdAsync(userId, subcategoryId);
        }
    }
}
