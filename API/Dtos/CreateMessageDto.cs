using System.ComponentModel.DataAnnotations;

namespace API.Dtos
{
    public class CreateMessageDto
    {
        [Required]
        public string RecipientUsername { get; set; }
        [Required]
        public string Content { get; set; }

        // NOU: optional, pentru mesajele legate de o comanda
        public int? OrderId { get; set; }
    }
}