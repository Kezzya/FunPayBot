using FunPayBot.src.Domain.Entities;
using FunPayBot.src.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace FunPayBot.src.Infrastructure.Persistence.Configuration
{
    public class LotConfiguration : IEntityTypeConfiguration<Lot>
    {
        public void Configure(EntityTypeBuilder<Lot> builder)
        {
            builder.HasKey(l => l.Id);
            builder.Property(l => l.FunPayLotId).IsRequired().HasMaxLength(50);
            builder.Property(l => l.CategoryUrl).IsRequired().HasMaxLength(500);
            builder.Property(l => l.Description).HasMaxLength(1000);
            builder.Property(l => l.IsActive);


            // Встраивание Price (Value Object)
            builder.OwnsOne(l => l.Price, p =>
            {
                p.Property(pr => pr.Value).HasColumnName("PriceValue").IsRequired();
                p.Property(pr => pr.Currency)
                    .HasConversion(
                        currency => currency.Code,
                        code => new Currency(code))
                    .HasColumnName("PriceCurrency")
                    .IsRequired()
                    .HasMaxLength(3);
            });
        }
    }
}
