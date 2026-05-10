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

        public MessageHub(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            UserManager<AppUser> userManager,
            IHubContext<NotificationHub> notificationHub)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userManager = userManager;
            _notificationHub = notificationHub;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"].ToString();
            var currentEmail = Context.User.RetrieveEmailFromPrincipal();

            var groupName = GetGroupName(currentEmail, otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            // Trimitem istoricul conversației
            var spec = new MessageThreadSpecification(currentEmail, otherUser);
            var messages = await _unitOfWork.Repository<Message>().ListAsync(spec);

            // Marcăm ca citite mesajele primite în această conversație
            var unreadMessages = messages
                .Where(m => m.RecipientUsername == currentEmail && m.DateRead == null)
                .ToList();

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                    msg.DateRead = DateTime.UtcNow;

                await _unitOfWork.Complete();

                // Actualizăm badge-ul în NotificationHub pentru userul curent
                await PushUnreadCount(currentEmail);
            }

            await Clients.Caller.SendAsync("ReceiveMessageThread", _mapper.Map<IEnumerable<MessageDto>>(messages));
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
                Content = createMessageDto.Content
            };

            _unitOfWork.Repository<Message>().Add(message);

            if (await _unitOfWork.Complete() > 0)
            {
                var groupName = GetGroupName(sender.Email, recipient.Email);
                await Clients.Group(groupName).SendAsync("NewMessage", _mapper.Map<MessageDto>(message));

                // Notificăm destinatarul că are un mesaj nou (actualizează badge-ul)
                await PushUnreadCount(recipient.Email);
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Calculează și trimite noul contor de necitite prin NotificationHub.
        /// </summary>
        private async Task PushUnreadCount(string recipientEmail)
        {
            var spec = new UnreadMessagesSpecification(recipientEmail);
            var count = await _unitOfWork.Repository<Message>().CountAsync(spec);

            // Trimitem tuturor conexiunilor active ale acelui user (poate fi pe mai multe tab-uri)
            await _notificationHub.Clients.User(recipientEmail).SendAsync("UnreadCount", count);
        }

        private string GetGroupName(string caller, string other)
        {
            var stringCompare = string.CompareOrdinal(caller, other) < 0;
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
        }
    }
}