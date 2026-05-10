using API.Dtos;
using API.Extensions;
using AutoMapper;
using Core.Entities;
using Core.Entities.Identity;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    [Authorize]
    public class MessageHub : Hub
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager;
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly IHubContext<PresenceHub> _presenceHub;

        public MessageHub(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            UserManager<AppUser> userManager,
            IHubContext<NotificationHub> notificationHub,
            IHubContext<PresenceHub> presenceHub)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userManager = userManager;
            _notificationHub = notificationHub;
            _presenceHub = presenceHub;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"].ToString();
            var currentEmail = Context.User.RetrieveEmailFromPrincipal();

            // NOU: citim orderId din query (optional: /hubs/message?user=bob@...&orderId=42)
            int? orderId = null;
            if (int.TryParse(httpContext.Request.Query["orderId"].ToString(), out var parsedOrderId))
                orderId = parsedOrderId;

            // Numele grupului include orderId daca exista, altfel e chat general
            var groupName = orderId.HasValue
                ? GetGroupName(currentEmail, otherUser) + $"_order_{orderId}"
                : GetGroupName(currentEmail, otherUser);

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            // NOU: aducem mesajele filtrate dupa orderId (sau fara orderId pentru chat general)
            var spec = orderId.HasValue
                ? new MessageThreadSpecification(currentEmail, otherUser, orderId.Value)
                : new MessageThreadSpecification(currentEmail, otherUser);

            var messages = await _unitOfWork.Repository<Message>().ListAsync(spec);

            // Marcam ca citite
            var unread = messages
                .Where(m => m.RecipientUsername == currentEmail && m.DateRead == null)
                .ToList();

            if (unread.Any())
            {
                unread.ForEach(m => m.DateRead = DateTime.UtcNow);
                await _unitOfWork.Complete();
                await PushUnreadCount(currentEmail);
            }

            await Clients.Caller.SendAsync("ReceiveMessageThread",
                _mapper.Map<IEnumerable<MessageDto>>(messages));
        }

        public async Task SendMessage(CreateMessageDto createMessageDto)
        {
            var email = Context.User.RetrieveEmailFromPrincipal();

            if (email == createMessageDto.RecipientUsername.ToLower())
                throw new HubException("Nu îți poți trimite mesaje ție însuți.");

            var sender = await _userManager.FindByEmailAsync(email);
            var recipient = await _userManager.FindByEmailAsync(createMessageDto.RecipientUsername);

            if (recipient == null) throw new HubException("Utilizatorul nu a fost găsit.");

            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.Email,
                RecipientUsername = recipient.Email,
                SenderId = sender.Id,
                RecipientId = recipient.Id,
                Content = createMessageDto.Content,
                // NOU: salvam OrderId pe mesaj daca exista
                OrderId = createMessageDto.OrderId
            };

            _unitOfWork.Repository<Message>().Add(message);

            if (await _unitOfWork.Complete() > 0)
            {
                // NOU: grupul include orderId daca exista
                var groupName = createMessageDto.OrderId.HasValue
                    ? GetGroupName(sender.Email, recipient.Email) + $"_order_{createMessageDto.OrderId}"
                    : GetGroupName(sender.Email, recipient.Email);

                await Clients.Group(groupName).SendAsync("NewMessage",
                    _mapper.Map<MessageDto>(message));

                await PushUnreadCount(recipient.Email);

                var recipientConnections = await PresenceTracker.GetConnectionsForUser(recipient.Email);
                if (recipientConnections.Any())
                {
                    await _presenceHub.Clients.Clients(recipientConnections).SendAsync(
                        "NewMessageReceived",
                        new { senderEmail = sender.Email, senderName = sender.DisplayName }
                    );
                }
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        private async Task PushUnreadCount(string recipientEmail)
        {
            var spec = new UnreadMessagesSpecification(recipientEmail);
            var count = await _unitOfWork.Repository<Message>().CountAsync(spec);
            await _notificationHub.Clients.User(recipientEmail).SendAsync("UnreadCount", count);
        }

        private string GetGroupName(string caller, string other)
        {
            var stringCompare = string.CompareOrdinal(caller, other) < 0;
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
        }
    }
}