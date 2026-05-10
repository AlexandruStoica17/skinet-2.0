namespace API.SignalR
{
    public class PresenceTracker
    {
        private static readonly Dictionary<string, List<string>> OnlineUsers = new();

        public Task UserConnected(string email, string connectionId)
        {
            lock (OnlineUsers)
            {
                if (OnlineUsers.ContainsKey(email))
                    OnlineUsers[email].Add(connectionId);
                else
                    OnlineUsers.Add(email, new List<string> { connectionId });
            }
            return Task.CompletedTask;
        }

        public Task UserDisconnected(string email, string connectionId)
        {
            lock (OnlineUsers)
            {
                if (!OnlineUsers.ContainsKey(email)) return Task.CompletedTask;

                OnlineUsers[email].Remove(connectionId);

                if (OnlineUsers[email].Count == 0)
                    OnlineUsers.Remove(email);
            }
            return Task.CompletedTask;
        }

        public static Task<List<string>> GetConnectionsForUser(string email)
        {
            List<string> connectionIds;
            lock (OnlineUsers)
            {
                connectionIds = OnlineUsers.TryGetValue(email, out var connections)
                    ? connections.ToList()
                    : new List<string>();
            }
            return Task.FromResult(connectionIds);
        }
    }
}