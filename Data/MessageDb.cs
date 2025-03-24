using MongoDB.Driver;
using Converse.Models;
using System.Threading.Tasks;

namespace Converse.Data
{
    public class MessageDb
    {
        private readonly IMongoCollection<MessageData> _messages;

        public MessageDb(IMongoDatabase database)
        {
            _messages = database.GetCollection<MessageData>("messages");
        }

        // Save a message asynchronously
        public async Task SaveMessageAsync(MessageData message)
        {
            await _messages.InsertOneAsync(message);
        }

        // Get message history between two users
        public async Task<List<MessageData>> GetMessageHistoryAsync(string user1, string user2)
        {
            var filter = Builders<MessageData>.Filter.Or(
                Builders<MessageData>.Filter.And(
                    Builders<MessageData>.Filter.Eq(msg => msg.SenderPhone, user1),
                    Builders<MessageData>.Filter.Eq(msg => msg.ReceiverPhone, user2)
                ),
                Builders<MessageData>.Filter.And(
                    Builders<MessageData>.Filter.Eq(msg => msg.SenderPhone, user2),
                    Builders<MessageData>.Filter.Eq(msg => msg.ReceiverPhone, user1)
                )
            );

            return await _messages.Find(filter)
                .Sort(Builders<MessageData>.Sort.Ascending(msg => msg.SentAt))
                .ToListAsync();
        }

        // Get group message history
        public async Task<List<MessageData>> GetGroupMessageHistoryAsync(string groupId)
        {
            var filter = Builders<MessageData>.Filter.Eq(msg => msg.GroupID, groupId);
            return await _messages.Find(filter)
                .Sort(Builders<MessageData>.Sort.Ascending(msg => msg.SentAt))
                .ToListAsync();
        }

        // Get unread messages for a user
        public async Task<List<MessageData>> GetUnreadMessagesAsync(string userId)
        {
            var filter = Builders<MessageData>.Filter.And(
                Builders<MessageData>.Filter.Eq(msg => msg.ReceiverPhone, userId),
                Builders<MessageData>.Filter.Eq(msg => msg.Read, false)
            );
            Console.WriteLine("DB hit for unread msg");
            return await _messages.Find(filter)
                .Sort(Builders<MessageData>.Sort.Ascending(msg => msg.SentAt))
                .ToListAsync();
        }

        // Mark messages as read
        public async Task<List<string>> MarkMessagesAsReadAsync(string userId, string senderPhone)
        {
            var filter = Builders<MessageData>.Filter.And(
                Builders<MessageData>.Filter.Eq(msg => msg.ReceiverPhone, userId),
                Builders<MessageData>.Filter.Eq(msg => msg.SenderPhone, senderPhone),
                Builders<MessageData>.Filter.Eq(msg => msg.Read, false)
            );

            var update = Builders<MessageData>.Update.Set(msg => msg.Read, true);

            // Perform update and check affected document count
            var updateResult = await _messages.UpdateManyAsync(filter, update);

            if (updateResult.ModifiedCount > 0)
            {
                var updatedMessages = await _messages.Find(filter).ToListAsync();
                return updatedMessages.Select(msg => msg.Id).ToList();
            }

            return new List<string>(); // Return empty list if no updates were made
        }

        // Mark messages as delivered
        public async Task MarkMessagesAsDeliveredAsync(string userId)
        {
            var filter = Builders<MessageData>.Filter.And(
                Builders<MessageData>.Filter.Eq(msg => msg.ReceiverPhone, userId),
                Builders<MessageData>.Filter.Eq(msg => msg.Delivered, false)
            );

            var update = Builders<MessageData>.Update.Set(msg => msg.Delivered, true);
            await _messages.UpdateManyAsync(filter, update);
        }
    }
}