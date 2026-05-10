using System.ComponentModel.DataAnnotations;

namespace API.Dtos
{
    public class ReviewDto
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        public string ProducerEmail { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string Comment { get; set; }
    }
}