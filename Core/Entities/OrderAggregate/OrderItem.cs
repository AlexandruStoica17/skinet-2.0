namespace Core.Entities.OrderAggregate
{
    public class OrderItem : BaseEntity
    {
         public OrderItem()
        {
        }

        // --- AM ACTUALIZAT CONSTRUCTORUL ---
        public OrderItem(ProductItemOrdered itemOrdered, decimal price, int quantity, string producerId, string producerName)
        {
            ItemOrdered = itemOrdered;
            Price = price;
            Quantity = quantity;
            ProducerId = producerId;
            ProducerName = producerName;
        }

        public ProductItemOrdered ItemOrdered { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }

        // --- PROPRIETĂȚI NOI PENTRU MARKETPLACE ---
        public string ProducerId { get; set; }
        public string ProducerName { get; set; }
    }
}