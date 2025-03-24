using Converse.Services.Logs;
using Converse.Services.Chat;
using Converse.Services.Group;
using Converse.Services.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Net.Mime;

namespace Converse.Hubs
{
    [Authorize] // Require authentication before connection
    public class ChatHub : Hub
    {
        private readonly ChatService _chatService;
        private readonly ConnectionService _connectionService;
        private readonly UserManagementService _userManagementService;
        private readonly GroupManagementService _groupManagementService;
        private readonly string _logFile = "logs/chat_hub.log";

        public ChatHub(ChatService chatService, ConnectionService connectionService, UserManagementService userManagementService, GroupManagementService groupManagementService)
        {
            _chatService = chatService;
            _connectionService = connectionService;
            _userManagementService = userManagementService;
            _groupManagementService = groupManagementService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = _connectionService.GetUserId(Context);
            if (string.IsNullOrWhiteSpace(userId))
            {
                LoggingService.Log(_logFile, "Connection rejected: User ID is null.");
                Context.Abort();
                return;
            }

            // Ensure SignalR can find users
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            _connectionService.AddConnection(userId, Context.ConnectionId);
            LoggingService.Log(_logFile, $"User {userId} Connected with ConnectionId {Context.ConnectionId}");

            // Rejoin all groups the user is a member of
            var groups = _userManagementService.GetUserGroups(userId);
            foreach (var groupId in groups)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
            }

            LoggingService.Log(_logFile, $"User {userId} rejoined groups: {string.Join(", ", groups)}");

            await base.OnConnectedAsync();
        }


        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = _connectionService.RemoveConnection(Context.ConnectionId);
            if (!string.IsNullOrEmpty(userId))
            {
                // Get groups before removing user
                var groups = _userManagementService.GetUserGroups(userId);
                foreach (var groupId in groups)
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
                }

                LoggingService.Log(_logFile, $"User {userId} disconnected and left groups: {string.Join(", ", groups)}");
            }

            await base.OnDisconnectedAsync(exception);
        }


        public async Task GetConnectedUsers()
        {
            var users = _connectionService.GetOnlineUsers(); // Ensure this function exists
            LoggingService.Log(_logFile, $"Connected users: {string.Join(", ", users)}");

            // Send the list of connected users to the caller
            await Clients.Caller.SendAsync("ReceiveConnectedUsers", users);
        }


        public async Task SendMessageToUser(string receiver, string messageContent)
        {
            var sender = _connectionService.GetUserId(Context);
            var receiverId = _connectionService.GetConnectionId(receiver);

            if (_userManagementService.GetUser(receiver) == null)
            {
                LoggingService.Log(_logFile, "Invalid Receiver. Message Not Sent.");
                return;
            }

            if (string.IsNullOrWhiteSpace(sender)){
                LoggingService.Log(_logFile, "Sender id is null or empty. Message not sent.");
                return;
            }

            LoggingService.Log(_logFile, $"Attempting to Send msg from {sender} to {receiver}");

            var sent = await _chatService.SendMessageAsync(sender, receiver, messageContent, "Text");
            LoggingService.Log(_logFile, $"SendMessageAsync result: {sent}");

            if (sent)
            {
                if (string.IsNullOrWhiteSpace(receiverId)){
                    LoggingService.Log(_logFile, "Reciever id is null or empty. Message not sent via SignalR but Saved in DB.");
                    return;
                }
                await Clients.Client(receiverId).SendAsync("ReceiveMessage", sender, messageContent);
                LoggingService.Log(_logFile, $"Message sent from {sender} to {receiver} via SignalR.");
            }
            else
            {
                LoggingService.Log(_logFile, $"Failed to send message from {sender} to {receiver}.");
            }
        }
        
        public async Task SendMessageToGroup(string groupId, string messageContent)
        {
            var sender = _connectionService.GetUserId(Context);

            if (string.IsNullOrWhiteSpace(sender))
            {
                LoggingService.Log(_logFile, "Sender ID is null or empty. Message not sent.");
                return;
            }

            var group = _groupManagementService.GetGroupById(groupId);

            if (group == null)
            {
                LoggingService.Log(_logFile, "Invalid Group ID. Message Not Sent.");
                return;
            }

            // Check if sender is a member of the group
            if (!group.Members.Contains(sender))
            {
                LoggingService.Log(_logFile, $"Unauthorized attempt: {sender} tried to send message to group {groupId}.");
                return;
            }

            var sent = await _chatService.SendGroupMessageAsync(sender, groupId, messageContent, "Text");
            LoggingService.Log(_logFile, $"SendMessageAsync result: {sent}");

            if (sent)
            {
                await Clients.Group(groupId).SendAsync("ReceiveGroupMessage", sender, messageContent, groupId);
                LoggingService.Log(_logFile, $"Group message sent from {sender} to {groupId}.");
            }
            else
            {
                LoggingService.Log(_logFile, $"Failed to send group message from {sender} to {groupId}.");
            }
        }

        public async Task JoinGroup(string groupId)
        {
            var userId = _connectionService.GetUserId(Context);

            if (string.IsNullOrWhiteSpace(userId))
            {
                LoggingService.Log(_logFile, "JoinGroup failed: User ID is null.");
                return;
            }

            var group = _groupManagementService.GetGroupById(groupId);

            if (group == null || !group.Members.Contains(userId))
            {
                LoggingService.Log(_logFile, $"JoinGroup failed: User {userId} is not a member of group {groupId}.");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
            _connectionService.AddUserToGroup(groupId, userId); // Track group membership
            LoggingService.Log(_logFile, $"User {userId} joined group {groupId}.");
        }

        public async Task LeaveGroup(string groupId)
        {
            var userId = _connectionService.GetUserId(Context);

            if (string.IsNullOrWhiteSpace(userId))
            {
                LoggingService.Log(_logFile, "LeaveGroup failed: User ID is null.");
                return;
            }

            var group = _groupManagementService.GetGroupById(groupId);

            if (group == null || !group.Members.Contains(userId))
            {
                LoggingService.Log(_logFile, $"LeaveGroup failed: User {userId} is not a member of group {groupId}.");
                return;
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
            _connectionService.RemoveUserFromGroup(groupId, userId); // Remove from tracking
            LoggingService.Log(_logFile, $"User {userId} left group {groupId}.");
        }

        public async Task NotifyJoiningGroup(string groupId, string userPhone)
        {
            var sender = _connectionService.GetUserId(Context);

            if (string.IsNullOrWhiteSpace(sender) || string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(userPhone)){
                LoggingService.Log(_logFile, "ID is null or empty. Invalid Join Operation.");
                return;
            }
            var userId = _connectionService.GetConnectionId(userPhone);

            var info = $"{sender} has Joined this Group";
            var notify = await _chatService.SendGroupMessageAsync(sender, groupId, info, "Info");

            if (notify)
            {
                var groupName = _groupManagementService.GetGroupById(groupId).GroupName;
                await Clients.Client(userId).SendAsync("NotifyJoinGroup", sender, info, groupId, groupName);
                LoggingService.Log(_logFile, $"Join Notify Message sent to {sender} via SignalR.");
            }
            else
            {
                LoggingService.Log(_logFile, $"Failed to send message to {sender}.");
            }

            LoggingService.Log(_logFile, $"JoinNotifyMessage result: {notify}");
        }
        public async Task NotifyLeavingGroup(string groupId, string userPhone)
        {
            var sender = _connectionService.GetUserId(Context);

            if (string.IsNullOrWhiteSpace(sender) || string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(userPhone)){
                LoggingService.Log(_logFile, "ID is null or empty. Invalid Leave Operation.");
                return;
            }
            var userId = _connectionService.GetConnectionId(userPhone);

            var info = $"{sender} has Left this Group";
            var notify = await _chatService.SendGroupMessageAsync(sender, groupId, info, "Info");

            if (notify)
            {
                await Clients.Client(userId).SendAsync("NotifyLeaveGroup", sender, info, groupId);
                LoggingService.Log(_logFile, $"Leave Notify Message sent to {sender} via SignalR.");
            }
            else
            {
                LoggingService.Log(_logFile, $"Failed to send message to {sender}.");
            }

            LoggingService.Log(_logFile, $"LeaveNotifyMessage result: {notify}");
        }
    }
}