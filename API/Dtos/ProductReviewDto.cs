using System.ComponentModel.DataAnnotations;

namespace API.Dtos
{
    public class ProductReviewDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string Comment { get; set; }
    }
}