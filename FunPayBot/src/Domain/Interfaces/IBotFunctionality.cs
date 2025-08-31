namespace FunPayBot.src.Domain.Interfaces
{
    public abstract class IBotFunctionality
    {
        public string Name { get; set; } // Название функциональности
        public bool IsActive { get; set; } // Статус активации
        public abstract Task ExecuteAsync(); // Основной метод выполнения
    }
}
