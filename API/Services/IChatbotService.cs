using API.Dtos;

namespace API.Services
{
    public interface IChatbotService
    {
        Task<ChatbotResponseDto> AskAsync(
            ChatbotRequestDto request,
            string userEmail,
            CancellationToken cancellationToken);
    }
}
