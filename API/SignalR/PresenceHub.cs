using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    [Authorize]
    public class PresenceHub : Hub
    {
        private readonly PresenceTracker _tracker;

        public PresenceHub(PresenceTracker tracker)
        {
            _tracker = tracker;
        }

        public override async Task OnConnectedAsync()
        {
            var email = Context.User.RetrieveEmailFromPrincipal();
            await _tracker.UserConnected(email, Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var email = Context.User.RetrieveEmailFromPrincipal();
            await _tracker.UserDisconnected(email, Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}