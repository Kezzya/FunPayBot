using System.Collections.Generic;
using FunPayBot.src.Domain.Entities;

namespace FunPayBot.src.Domain.Interfaces
{
    public interface IMessageRepository { 
            Task GetByIdAsync(Guid id); 
            Task GetByChatIdAsync(string chatId);
            Task AddAsync(Message message);
            Task DeleteAsync(Guid id);
        }
    }
