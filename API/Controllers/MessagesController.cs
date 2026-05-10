using API.Dtos;
using API.Extensions;
using Core.Entities;
using Core.Entities.Identity;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class MessagesController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;

        public MessagesController(IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        // GET /api/messages/inbox
        // Returnează lista conversațiilor unice, cu ultimul mesaj și numărul de necitite
        [HttpGet("inbox")]
        public async Task<ActionResult<IEnumerable<ConversationDto>>> GetInbox()
        {
            var currentEmail = User.RetrieveEmailFromPrincipal();

            var spec = new AllUserMessagesSpecification(currentEmail);
            var allMessages = await _unitOfWork.Repository<Message>().ListAsync(spec);

            // Grupăm după partenerul de conversație
            var conversations = allMessages
                .GroupBy(m =>
                    m.SenderUsername == currentEmail
                        ? m.RecipientUsername
                        : m.SenderUsername
                )
                .Select(group =>
                {
                    var partnerEmail = group.Key;
                    var lastMessage = group.First(); // spec are OrderByDescending deja

                    var partnerUser = lastMessage.SenderUsername == currentEmail
                        ? lastMessage.Recipient
                        : lastMessage.Sender;

                    var unreadCount = group.Count(m =>
                        m.RecipientUsername == currentEmail && m.DateRead == null);

                    return new ConversationDto
                    {
                        PartnerEmail = partnerEmail,
                        PartnerName = partnerUser?.DisplayName ?? partnerEmail,
                        LastMessage = lastMessage.Content,
                        LastMessageSent = lastMessage.MessageSent,
                        UnreadCount = unreadCount
                    };
                })
                .OrderByDescending(c => c.LastMessageSent)
                .ToList();

            return Ok(conversations);
        }
    }
}