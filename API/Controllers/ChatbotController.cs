using API.Dtos;
using API.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class ChatbotController : BaseApiController
    {
        private readonly IChatbotService _chatbotService;

        public ChatbotController(IChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        [HttpPost("ask")]
        public async Task<ActionResult<ChatbotResponseDto>> Ask(
            ChatbotRequestDto request,
            CancellationToken cancellationToken)
        {
            var userEmail = User?.Identity?.IsAuthenticated == true ? User.Identity.Name : null;
            var response = await _chatbotService.AskAsync(request, userEmail, cancellationToken);

            return Ok(response);
        }
    }
}
