using FunPayBot.src.Domain.Interfaces;

namespace FunPayBot.src.Domain.Entities
{
    public class DefaultFeature : IFunPayBotFeature
    {
        public string Name { get; set; } = "Default Feature";
        public bool IsActive { get; set; } = true;

        public override Task ExecuteAsync()
        {
            // Placeholder or default behavior
            return Task.CompletedTask;
        }
 
    }
}
