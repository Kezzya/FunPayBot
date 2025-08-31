using FunPayBot.src.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using FunPayBot.src.Domain.ValueObjects;

namespace FunPayBot.src.Infrastructure.Persistence.Configuration
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasKey(o => o.Id);

            builder.Property(o => o.FunPayOrderId)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(o => o.CreatedAt)
                   .IsRequired();

            // Настройка отношения с Lot
            builder.HasOne(o => o.Lot)
                   .WithMany() // или WithMany(l => l.Orders) если у Lot есть коллекция Orders
                   .HasForeignKey("LotId") // Теневое свойство
                   .OnDelete(DeleteBehavior.Restrict);

            // Настройка отношения с User (Buyer)
            builder.HasOne(o => o.Buyer)
                   .WithMany() // или WithMany(u => u.Orders) если у User есть коллекция Orders
                   .HasForeignKey("BuyerId") // Теневое свойство
                   .OnDelete(DeleteBehavior.Restrict);

            // Встраивание Amount (Value Object Price)
            builder.OwnsOne(o => o.Amount, amount =>
            {
                amount.Property(a => a.Value)
                      .HasColumnName("Amount")
                      .IsRequired()
                      .HasColumnType("decimal(18,2)");

                amount.Property(a => a.Currency)
                      .HasConversion(
                          currency => currency.Code,
                          code => new Currency(code))
                      .HasColumnName("AmountCurrency")
                      .IsRequired()
                      .HasMaxLength(3);
            });

            // Встраивание Status (Value Object OrderStatus)
            builder.OwnsOne(o => o.Status, status =>
            {
                status.Property(s => s.Status) // или как у вас называется свойство в OrderStatus
                      .HasColumnName("Status")
                      .IsRequired()
                      .HasMaxLength(50);
            });
        }
    }
}
