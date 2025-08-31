using FunPayBot.src.Domain.Entities;

namespace FunPayBot.src.Domain.ValueObjects
{
    public class Price
    {
        public decimal Value { get;  set; }
        public Currency Currency { get;  set; }

        public Price(decimal value, Currency currency)
        {
            if (value < 0) throw new ArgumentException("Price cannot be negative", nameof(value));
            Value = value;
            Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        }

        public override bool Equals(object obj)
        {
            if (obj is Price other)
            {
                return Value == other.Value && Currency.Equals(other.Currency);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, Currency);
        }
    }
}

