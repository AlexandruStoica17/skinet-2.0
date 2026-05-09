using System.Linq;
using Core.Entities.OrderAggregate;

namespace Core.Specifications
{
    public class OrdersForProducerSpecification : BaseSpecification<Order>
    {
        // Acest filtru aduce doar comenzile care au măcar un produs al acestui vânzător
        public OrdersForProducerSpecification(string producerId) 
            : base(o => o.OrderItems.Any(i => i.ProducerId == producerId))
        {
            AddInclude(o => o.OrderItems);
            AddInclude(o => o.DeliveryMethod);
            AddOrderByDescending(o => o.OrderDate);
        }
    }
}