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
    [Authorize] // Ne asigurăm că doar utilizatorii logați ajung aici
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

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"].ToString(); 
            // Folosim extensia ta pentru Email în loc de Username
            var currentEmail = Context.User.RetrieveEmailFromPrincipal(); 

            var groupName = GetGroupName(currentEmail, otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            var spec = new MessageThreadSpecification(currentEmail, otherUser);
            var messages = await _unitOfWork.Repository<Message>().ListAsync(spec);

            await Clients.Caller.SendAsync("ReceiveMessageThread", _mapper.Map<IEnumerable<MessageDto>>(messages));
        }

        public async Task SendMessage(CreateMessageDto createMessageDto)
        {
            var email = Context.User.RetrieveEmailFromPrincipal(); 

            if (email == createMessageDto.RecipientUsername.ToLower())
                throw new HubException("You cannot send messages to yourself");

            // Căutăm după Email, nu după Nume
            var sender = await _userManager.FindByEmailAsync(email);
            var recipient = await _userManager.FindByEmailAsync(createMessageDto.RecipientUsername);

            if (recipient == null) throw new HubException("Not found user");

            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.Email, // Salvăm emailul în ambele părți
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