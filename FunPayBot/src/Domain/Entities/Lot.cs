using FunPayBot.src.Domain.ValueObjects;

namespace FunPayBot.src.Domain.Entities
{
    public class Lot
    {
        public Guid Id { get; private set; }
        public string FunPayLotId { get; private set; }
        public Price Price { get; private set; }
        public string CategoryUrl { get; private set; }
        public string Description { get; internal set; }
        public bool? IsActive { get; internal set; }
        private Lot() { }
        public Lot(string funPayLotId, Price price, string categoryUrl)
        {
            Id = Guid.NewGuid();
            FunPayLotId = funPayLotId;
            Price = price ?? throw new ArgumentNullException(nameof(price));
            CategoryUrl = categoryUrl;
        }

        public void UpdatePrice(Price newPrice)
        {
            Price = newPrice ?? throw new ArgumentNullException(nameof(newPrice));
        }
    }

}
