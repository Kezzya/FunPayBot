using Microsoft.EntityFrameworkCore;
using FunPayBot.src.Domain.Entities;
using FunPayBot.src.Infrastructure.Persistence.Configuration;
using System.Collections.Generic;

namespace FunPayBot.src.Infrastructure.Persistence
{
    public class FunPayBotDbContext : DbContext
    {
        public DbSet<Lot> Lots { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<User> Users { get; set; }

        public FunPayBotDbContext(DbContextOptions<FunPayBotDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new LotConfiguration());
            modelBuilder.ApplyConfiguration(new OrderConfiguration());
            modelBuilder.ApplyConfiguration(new MessageConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());

        }
    }
}
