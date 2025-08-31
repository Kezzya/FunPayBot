using FunPayBot.src.Domain.Entities;
using FunPayBot.src.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace FunPayBot.src.Infrastructure.Persistence.Configuration
{
    public class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Content).IsRequired().HasMaxLength(2000);
            builder.Property(m => m.SentAt).IsRequired().HasColumnType("timestamp");
            builder.Property(m => m.FunPayMessageId).IsRequired();
            builder.Property(m => m.ChatId).IsRequired();

        }
    }
}
