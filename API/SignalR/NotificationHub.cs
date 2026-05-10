using API.Extensions;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Core.Entities.Identity;

namespace API.SignalR
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;

        public NotificationHub(IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        /// <summary>
        /// La conectare, trimitem imediat contorul de mesaje necitite.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var email = Context.User.RetrieveEmailFromPrincipal();
            var unreadCount = await GetUnreadCount(email);

            // Trimitem contorul DOAR clientului care s-a conectat
            await Clients.Caller.SendAsync("UnreadCount", unreadCount);
        }

        /// <summary>
        /// Numărul de mesaje primite dar necitite (DateRead == null).
        /// </summary>
        private async Task<int> GetUnreadCount(string recipientEmail)
        {
            var spec = new UnreadMessagesSpecification(recipientEmail);
            return await _unitOfWork.Repository<Message>().CountAsync(spec);
        }
    }
}