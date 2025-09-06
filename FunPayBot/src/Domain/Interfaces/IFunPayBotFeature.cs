namespace FunPayBot.src.Domain.Interfaces
{
    public interface IFunPayBotFeature
    {
        string Name { get; set; } // Название функциональности
        bool IsActive { get; set; } // Статус активации
        Task ExecuteAsync(); // Основной метод выполнения
    }
}
