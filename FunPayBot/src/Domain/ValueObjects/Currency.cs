namespace FunPayBot.src.Domain.ValueObjects
{
    public class Currency
    {
        public string Code { get; private set; }

        public Currency(string code)
        {
            if (string.IsNullOrEmpty(code)) throw new ArgumentException("Currency code cannot be empty", nameof(code));
            Code = code.ToUpper();
        }

        public override bool Equals(object obj)
        {
            if (obj is Currency other)
            {
                return Code == other.Code;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Code.GetHashCode();
        }
    }
}
