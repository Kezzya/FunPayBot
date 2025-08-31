using FunPayBot.src.Domain.Entities;

namespace FunPayBot.src.Domain.Interfaces
{
    public interface IUserRepository
    { 
        Task GetByIdAsync(Guid id); 
        Task GetByFunPayUserIdAsync(string funPayUserId); 
        Task AddAsync(User user); 
        Task UpdateAsync(User user); 
        Task DeleteAsync(Guid id);
    }
}
