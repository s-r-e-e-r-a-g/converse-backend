using Converse.Events;
using Converse.Models;
using Converse.Services.Logs;
using Converse.Services.Group;
using Converse.Services.Message;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Converse.Services.Chat
{
    public class ChatService
    {
        private readonly MessageService _messageService;
        private readonly GroupManagementService _groupManagementService;
        private readonly ConnectionService _connectionService;
        private readonly IEventBus _eventBus;
        private readonly string _logFile = "logs/chat_service.log";

        public ChatService(MessageService messageService, GroupManagementService groupManagementService, ConnectionService connectionService, IEventBus eventBus)
        {
            _messageService = messageService;
            _groupManagementService = groupManagementService;
            _connectionService = connectionService;
            _eventBus = eventBus;
        }

        public async Task<bool> SendMessageAsync(string senderPhone, string receiverPhone, string content, string contentType)
        {
            var message = new MessageData
            {
                SenderPhone = senderPhone,
                ReceiverPhone = receiverPhone,
                GroupID = null,
                Content = content,
                ContentType = contentType,
                SentAt = DateTime.UtcNow,
                Read = false,
                Delivered = false
            };

            var saved = await _messageService.SaveAndSendMessageAsync(message);
            if (!saved) 
            {
                LoggingService.Log(_logFile, "Failed to save message.");
                return false;
            }

            await _eventBus.Publish(new MessageSentEvent(senderPhone, receiverPhone, content));
            LoggingService.Log(_logFile, $"Message from {senderPhone} to {receiverPhone} published to event bus.");

            if (_connectionService.IsUserOnline(receiverPhone))
            {
                await _messageService.MarkMessagesAsDeliveredAsync(receiverPhone);
            }

            return true;
        }

        public async Task<bool> SendGroupMessageAsync(string senderPhone, string groupId, string content, string contentType)
        {
            var group = _groupManagementService.GetGroupById(groupId);
            if (group == null || !group.Members.Contains(senderPhone))
            {
                LoggingService.Log(_logFile, $"Failed to send group message: Sender {senderPhone} is not in group {groupId}.");
                return false;
            }

            var message = new MessageData
            {
                SenderPhone = senderPhone,
                GroupID = groupId,
                Content = content,
                ContentType = contentType,
                SentAt = DateTime.UtcNow,
                Read = false,
                Delivered = false
            };

            var saved = await _messageService.SaveAndSendMessageAsync(message);
            if (!saved)
            {
                LoggingService.Log(_logFile, "Failed to save group message.");
                return false;
            }

            await _eventBus.Publish(new GroupMessageSentEvent(senderPhone, groupId, content));
            LoggingService.Log(_logFile, $"Group message from {senderPhone} to group {groupId} published to event bus.");

            return true;
        }
    }
}