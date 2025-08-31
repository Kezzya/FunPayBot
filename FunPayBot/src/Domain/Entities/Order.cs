using FunPayBot.src.Domain.ValueObjects;

namespace FunPayBot.src.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; private set; }
        public string FunPayOrderId { get; private set; }
        public Lot Lot { get; private set; }
        public User Buyer { get; private set; }
        public Price Amount { get; private set; }
        public OrderStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public Order()
        {
            
        }
        public Order(string funPayOrderId, Lot lot, User buyer, Price amount, OrderStatus status)
        {
            Id = Guid.NewGuid();
            FunPayOrderId = funPayOrderId ?? throw new ArgumentNullException(nameof(funPayOrderId));
            Lot = lot ?? throw new ArgumentNullException(nameof(lot));
            Buyer = buyer ?? throw new ArgumentNullException(nameof(buyer));
            Amount = amount ?? throw new ArgumentNullException(nameof(amount));
            Status = status ?? throw new ArgumentNullException(nameof(status));
            CreatedAt = DateTime.UtcNow;
        }

        public void UpdateStatus(OrderStatus newStatus)
        {
            Status = newStatus ?? throw new ArgumentNullException(nameof(newStatus));
        }
    }
}
