using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace API.SignalR
{
    public class PresenceTracker
    {
        SemaphoreSlim _semaphoregate = new SemaphoreSlim(1);

        private static readonly Dictionary<string, List<string>> OnlineUsers
            = new Dictionary<string, List<string>>();

        public async Task<bool> UserConnected(string username, string connectionId)
        {
            bool isOnline = false;
            await _semaphoregate.WaitAsync();

            if (OnlineUsers.ContainsKey(username))
            {
                OnlineUsers[username].Add(connectionId);
            }
            else
            {
                OnlineUsers.Add(username, new List<string> { connectionId });
                isOnline = true;
            }

            _semaphoregate.Release();

            return isOnline;
        }

        public async Task<bool> UserDisconnected(string username, string connectionId)
        {
            bool isOffline = false;
            await _semaphoregate.WaitAsync();

            if (!OnlineUsers.ContainsKey(username))
                return isOffline;

            OnlineUsers[username].Remove(connectionId);
            if (OnlineUsers[username].Count == 0)
            {
                OnlineUsers.Remove(username);
                isOffline = true;
            }

            _semaphoregate.Release();

            return isOffline;
        }

        public async Task<string[]> GetOnlineUsers()
        {
            string[] onlineUsers;
            await _semaphoregate.WaitAsync();

            onlineUsers = OnlineUsers.OrderBy(k => k.Key).Select(k => k.Key).ToArray();

            _semaphoregate.Release();

            return onlineUsers;
        }

        public async Task<List<string>> GetConnectionsForUser(string username)
        {
            List<string> connectionIds;
            await _semaphoregate.WaitAsync();

            connectionIds = OnlineUsers.GetValueOrDefault(username);

            _semaphoregate.Release();

            return connectionIds;
        }
    }
}