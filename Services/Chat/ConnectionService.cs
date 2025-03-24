using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Converse.Services.Chat
{
    public class ConnectionService
    {
        private static readonly HashSet<string> _onlineUsers = new();
        private static readonly ConcurrentDictionary<string, string> _userConnections = new();
        private static readonly ConcurrentDictionary<string, HashSet<string>> _groupConnections = new();

        public void AddConnection(string userId, string connectionId)
        {
            _userConnections[userId] = connectionId;
            lock (_onlineUsers)
            {
                _onlineUsers.Add(userId);
            }
            Console.WriteLine($"Connection Service Message: User {userId} connected with ConnectionId: {connectionId}");
        }

        public string RemoveConnection(string connectionId)
        {
            var userId = _userConnections.FirstOrDefault(x => x.Value == connectionId).Key;
            if (!string.IsNullOrEmpty(userId))
            {
                _userConnections.TryRemove(userId, out _);
                lock (_onlineUsers)
                {
                    _onlineUsers.Remove(userId);
                }
                Console.WriteLine($"Connection Service Message: User {userId} disconnected.");
            }
            return userId;
        }

        public string GetConnectionId(string userId)
        {
            _userConnections.TryGetValue(userId, out var connectionId);
            return connectionId;
        }

        public string GetUserIdByConnection(string connectionId)
        {
            return _userConnections.FirstOrDefault(x => x.Value == connectionId).Key;
        }

        public bool IsUserOnline(string userId)
        {
            lock (_onlineUsers)
            {
                return _onlineUsers.Contains(userId);
            }
        }

        public List<string> GetOnlineUsers()
        {
            lock (_onlineUsers)
            {
                Console.WriteLine("Online Users");
                return _onlineUsers.ToList();
            }
        }

        public string GetUserId(HubCallerContext context)
        {
            return context.User?.FindFirst(ClaimTypes.Name)?.Value;
        }

        public void AddUserToGroup(string groupId, string userId)
        {
            if (string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(userId))
                return;

            _groupConnections.AddOrUpdate(groupId,
                new HashSet<string> { userId },
                (_, existingUsers) =>
                {
                    lock (existingUsers)
                    {
                        existingUsers.Add(userId);
                    }
                    return existingUsers;
                });

            Console.WriteLine($"User {userId} added to group {groupId}");
        }

        public void RemoveUserFromGroup(string groupId, string userId)
        {
            if (string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(userId))
                return;

            if (_groupConnections.TryGetValue(groupId, out var users))
            {
                lock (users)
                {
                    users.Remove(userId);
                    if (users.Count == 0)
                    {
                        _groupConnections.TryRemove(groupId, out _);
                    }
                }
                Console.WriteLine($"User {userId} removed from group {groupId}");
            }
        }

        public string GetSignalRGroupName(string groupId)
        {
            return _groupConnections.ContainsKey(groupId) ? groupId : null;
        }

        public List<string> GetUsersInGroup(string groupId)
        {
            return _groupConnections.TryGetValue(groupId, out var users)
                ? users.ToList()
                : new List<string>();
        }

    }
}