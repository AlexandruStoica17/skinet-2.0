using API.Dtos;
using API.Extensions;
using AutoMapper;
using Core.Entities;
using Core.Entities.Identity;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    public class MessageHub : Hub
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager;

        public MessageHub(IUnitOfWork unitOfWork, IMapper mapper, UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userManager = userManager;
        }

        // 1. Când utilizatorul deschide fereastra de chat
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"].ToString(); // Cu cine vorbește
            var currentUsername = Context.User.GetUsername(); // Cine este logat

            var groupName = GetGroupName(currentUsername, otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            // Aducem istoricul mesajelor dintre cei doi
            var spec = new MessageThreadSpecification(currentUsername, otherUser);
            var messages = await _unitOfWork.Repository<Message>().ListAsync(spec);

            // Trimitem istoricul înapoi la user-ul care s-a conectat
            await Clients.Caller.SendAsync("ReceiveMessageThread", _mapper.Map<IEnumerable<MessageDto>>(messages));
        }

        // 2. Metoda prin care un user TRIMITE un mesaj nou
        public async Task SendMessage(CreateMessageDto createMessageDto)
        {
            var username = Context.User.GetUsername();

            if (username == createMessageDto.RecipientUsername.ToLower())
                throw new HubException("You cannot send messages to yourself");

            var sender = await _userManager.FindByNameAsync(username);
            var recipient = await _userManager.FindByNameAsync(createMessageDto.RecipientUsername);

            if (recipient == null) throw new HubException("Not found user");

            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.UserName,
                RecipientUsername = recipient.UserName,
                SenderId = sender.Id,
                RecipientId = recipient.Id,
                Content = createMessageDto.Content
            };

            _unitOfWork.Repository<Message>().Add(message);

            if (await _unitOfWork.Complete() > 0)
            {
                var groupName = GetGroupName(sender.UserName, recipient.UserName);
                // Trimitem mesajul nou către toți cei din grupul de chat (ambii useri)
                await Clients.Group(groupName).SendAsync("NewMessage", _mapper.Map<MessageDto>(message));
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        private string GetGroupName(string caller, string other)
        {
            var stringCompare = string.CompareOrdinal(caller, other) < 0;
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
        }
    }
}