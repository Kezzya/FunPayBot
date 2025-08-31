namespace FunPayBot.src.Domain.ValueObjects
{
    public class OrderStatus
    {
        public string Status { get; private set; }

        public OrderStatus(string status)
        {
            if (string.IsNullOrEmpty(status)) throw new ArgumentException("Status cannot be empty", nameof(status));
            Status = status;
        }

        public static OrderStatus Pending => new OrderStatus("Pending");
        public static OrderStatus Paid => new OrderStatus("Paid");
        public static OrderStatus Completed => new OrderStatus("Completed");
        public static OrderStatus Cancelled => new OrderStatus("Cancelled");

        public override bool Equals(object obj)
        {
            if (obj is OrderStatus other)
            {
                return Status == other.Status;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Status.GetHashCode();
        }
    }
}
