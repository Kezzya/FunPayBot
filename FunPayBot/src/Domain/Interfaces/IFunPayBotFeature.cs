namespace FunPayBot.src.Domain.Interfaces
{
    public interface IFunPayBotFeature
    {
        string Name { get; set; }
        string Description { get; set; }
        string ViewName { get; set; }
        bool IsActive { get; set; } // Статус активации

        Task ExecuteAsync(); // Основной метод выполнения
    }
}
