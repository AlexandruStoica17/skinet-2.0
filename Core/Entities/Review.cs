using Core.Entities.Identity;

namespace Core.Entities
{
    public class Review : BaseEntity
    {
        public int OrderId { get; set; }
        public string BuyerEmail { get; set; }
        public string ProducerEmail { get; set; }
        public int Rating { get; set; }       // 1-5
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}