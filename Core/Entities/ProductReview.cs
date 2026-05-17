namespace Core.Entities
{
    public class ProductReview : BaseEntity
    {
        public int ProductId { get; set; }
        public int OrderId { get; set; }
        public string BuyerEmail { get; set; }
        public string BuyerName { get; set; }
        public int Rating { get; set; }       // 1-5
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}