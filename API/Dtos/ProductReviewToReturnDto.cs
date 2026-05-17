namespace API.Dtos
{
    public class ProductReviewToReturnDto
    {
        public int Id { get; set; }
        public string BuyerName { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}