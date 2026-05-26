using System.ComponentModel.DataAnnotations;

namespace API.Dtos
{
    public class ChatbotRequestDto
    {
        [Required]
        [StringLength(1200, MinimumLength = 2)]
        public string Message { get; set; }

        public IReadOnlyList<ChatbotHistoryMessageDto> History { get; set; } =
            Array.Empty<ChatbotHistoryMessageDto>();
    }
}
