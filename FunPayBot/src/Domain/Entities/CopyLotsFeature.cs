using FunPayBot.src.Domain.Interfaces;
using FunPayBot.src.Domain.Services;

namespace FunPayBot.src.Domain.Entities
{
    public class CopyLotsFeature : IFunPayBotFeature
    {
        private readonly LotCopyService _lotCopyService;

        public CopyLotsFeature(LotCopyService lotCopyService)
        {
            _lotCopyService = lotCopyService;
        }
        public string Name { get; set; } = "Copy Lots from user Feature";
        public bool IsActive { get; set; } = true;

        public override async Task ExecuteAsync()
        {
            // Пример: Копирование лотов для userId и subcategoryId

            int userId = 123; 
            int subcategoryId = 456; 
            await _lotCopyService.CopyLotsByUserIdAsync(userId, subcategoryId);
        }

    }
}
