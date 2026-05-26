using System.ComponentModel.DataAnnotations;

namespace API.Dtos
{
    public class ChatbotHistoryMessageDto
    {
        [Required]
        [StringLength(20)]
        public string Role { get; set; }

        [Required]
        [StringLength(1600)]
        public string Content { get; set; }
    }
}
