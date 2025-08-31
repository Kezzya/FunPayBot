using FunPayBot.src.Domain.Entities;
using System.Collections.Generic;

namespace FunPayBot.src.Domain.Interfaces
{
    public interface ILotRepository { 
        Task<Lot> GetByIdAsync(Guid id); 
        Task<Lot> GetByFunPayLotIdAsync(string funPayLotId); 
        Task <List<Lot>> GetAllAsync(); 
        Task AddAsync(Lot lot); 
        Task UpdateAsync(Lot lot); 
        Task DeleteAsync(Guid id); }
}

