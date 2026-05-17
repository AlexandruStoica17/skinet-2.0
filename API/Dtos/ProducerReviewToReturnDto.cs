namespace API.Dtos
{
    public class ProducerReviewToReturnDto
    {
        public int Id { get; set; }
        public string BuyerEmail { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProducerReviewsResponseDto
    {
        public List<ProducerReviewToReturnDto> Reviews { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
    }
}