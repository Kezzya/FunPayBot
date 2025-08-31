using FunPayBot.src.Domain.Entities;
using System.Collections.Generic;

namespace FunPayBot.src.Domain.Interfaces
{
    public interface IOrderRepository
    { 
        Task GetByIdAsync(Guid id); 
        Task GetByFunPayOrderIdAsync(string funPayOrderId); 
        Task GetByLotIdAsync(Guid lotId); 
        Task AddAsync(Order order); 
        Task UpdateAsync(Order order); 
        Task DeleteAsync(Guid id); 
    }
}
