using FunPayBot.src.Domain.Entities;
using FunPayBot.src.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FunPayBot.src.Infrastructure.Persistence.Repositories
{
    public class LotRepository : ILotRepository
    {
        private readonly FunPayBotDbContext _context;

        public LotRepository(FunPayBotDbContext context)
        {
            _context = context;
        }

        public async Task<Lot> GetByIdAsync(Guid id)
        {
            return await _context.Lots.FindAsync(id);
        }

        public async Task<Lot> GetByFunPayLotIdAsync(string funPayLotId)
        {
            return await _context.Lots.FirstOrDefaultAsync(l => l.FunPayLotId == funPayLotId);
        }

        public async Task<List<Lot>> GetAllAsync()
        {
            return await _context.Lots.ToListAsync();
        }

        public async Task AddAsync(Lot lot)
        {
            await _context.Lots.AddAsync(lot);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Lot lot)
        {
            _context.Lots.Update(lot);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var lot = await GetByIdAsync(id);
            if (lot != null)
            {
                _context.Lots.Remove(lot);
                await _context.SaveChangesAsync();
            }
        }
    }
}
