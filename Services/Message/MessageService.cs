using Converse.Data;
using Converse.Events;
using Converse.Models;
using Converse.Services.Logs;
using System.Threading.Tasks;

namespace Converse.Services.Message
{
    public class MessageService
    {
        private readonly MessageDb _messageDb;
        private readonly IEventBus _eventBus;
        private readonly string _logFile = "logs/message_service.log";

        public MessageService(MessageDb messageDb, IEventBus eventBus)
        {
            _messageDb = messageDb;
            _eventBus = eventBus;
        }

        public async Task<bool> SaveAndSendMessageAsync(MessageData message)
        {
            try
            {
                await _messageDb.SaveMessageAsync(message);
                await _eventBus.Publish(new MessageSavedEvent(message));
                LoggingService.Log(_logFile, $"Message saved: {message.SenderPhone} -> {message.ReceiverPhone}");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Log(_logFile, $"Error saving message: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> MarkMessagesAsReadAsync(string receiverPhone, string senderPhone)
        {
            var updatedMessageIds = await _messageDb.MarkMessagesAsReadAsync(receiverPhone, senderPhone);
            if (updatedMessageIds.Count > 0)
            {
                await _eventBus.Publish(new MessageMarkedAsReadEvent(receiverPhone, senderPhone));
                return true;
            }
            return false;
        }

        public async Task MarkMessagesAsDeliveredAsync(string userId)
        {
            await _messageDb.MarkMessagesAsDeliveredAsync(userId);
            await _eventBus.Publish(new MessageDeliveredEvent(userId));
        }

        public async Task<List<MessageData>> GetMessageHistoryAsync(string user1, string user2, bool isGroup)
        {
            if (isGroup)
            {
                return await _messageDb.GetGroupMessageHistoryAsync(user2);
            }
            return await _messageDb.GetMessageHistoryAsync(user1, user2);
        }
    }
}