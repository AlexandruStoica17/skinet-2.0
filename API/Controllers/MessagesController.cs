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

        // GET /api/messages/inbox?search=bob&pageIndex=1&pageSize=10
        [HttpGet("inbox")]
        public async Task<ActionResult<IEnumerable<ConversationDto>>> GetInbox(
            [FromQuery] string search = "",
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var currentEmail = User.RetrieveEmailFromPrincipal();

            var spec = new AllUserMessagesSpecification(currentEmail);
            var allMessages = await _unitOfWork.Repository<Message>().ListAsync(spec);

            // NOU: grupam dupa OrderId (fiecare comanda = conversatie separata)
            // Mesajele fara OrderId (chat general) sunt grupate dupa partener ca inainte
            var conversations = allMessages
                .GroupBy(m =>
                {
                    var partnerEmail = m.SenderUsername == currentEmail
                        ? m.RecipientUsername
                        : m.SenderUsername;
                    // Cheia grupului: orderId (daca exista) sau email partener (chat general)
                    return m.OrderId.HasValue
                        ? $"order_{m.OrderId}"
                        : $"chat_{partnerEmail}";
                })
                .Select(group =>
                {
                    var lastMessage = group.First(); // spec are OrderByDescending deja

                    var partnerEmail = lastMessage.SenderUsername == currentEmail
                        ? lastMessage.RecipientUsername
                        : lastMessage.SenderUsername;

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
                        UnreadCount = unreadCount,
                        // NOU: OrderId si titlu pentru afisare in inbox
                        OrderId = lastMessage.OrderId,
                        OrderTitle = lastMessage.OrderId.HasValue
                            ? $"Order #{lastMessage.OrderId}"
                            : null
                    };
                })
                .OrderByDescending(c => c.LastMessageSent)
                .ToList();

            // Filtram dupa search
            if (!string.IsNullOrWhiteSpace(search))
            {
                conversations = conversations
                    .Where(c =>
                        c.PartnerName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        c.PartnerEmail.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (c.OrderTitle != null && c.OrderTitle.Contains(search, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            // Paginare
            var totalCount = conversations.Count;
            var paged = conversations
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            Response.Headers.Add("X-Pagination-Total", totalCount.ToString());

            return Ok(paged);
        }

        // POST /api/messages/review
        [HttpPost("review")]
        public async Task<ActionResult> SubmitReview(ReviewDto reviewDto)
        {
            var buyerEmail = User.RetrieveEmailFromPrincipal();

            var existingReviews = await _unitOfWork.Repository<Review>().ListAllAsync();
            var alreadyReviewed = existingReviews.Any(r =>
                r.OrderId == reviewDto.OrderId && r.BuyerEmail == buyerEmail);

            if (alreadyReviewed)
                return BadRequest(new { message = "You have already left a review for this order." });

            var review = new Review
            {
                OrderId = reviewDto.OrderId,
                BuyerEmail = buyerEmail,
                ProducerEmail = reviewDto.ProducerEmail,
                Rating = reviewDto.Rating,
                Comment = reviewDto.Comment
            };

            _unitOfWork.Repository<Review>().Add(review);
            await _unitOfWork.Complete();

            return Ok(new { message = "Review submitted successfully! Thank you!" });
        }

        // GET /api/messages/search-user?query=bob
        [HttpGet("search-user")]
        public async Task<ActionResult<IEnumerable<UserSearchResultDto>>> SearchUser([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Ok(new List<UserSearchResultDto>());

            var currentEmail = User.RetrieveEmailFromPrincipal();

            var users = _userManager.Users
                .Where(u =>
                    u.Email != currentEmail &&
                    (u.Email.Contains(query) || u.DisplayName.Contains(query)))
                .Take(10)
                .Select(u => new UserSearchResultDto
                {
                    Email = u.Email,
                    DisplayName = u.DisplayName
                })
                .ToList();

            return Ok(users);
        }
    }
}
