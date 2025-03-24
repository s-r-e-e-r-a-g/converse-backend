using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Converse.Models
{
    public class MessageData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; } // MongoDB identifier
        public string SenderPhone { get; set; } // Sender's phone number
        public string? ReceiverPhone { get; set; } // Receiver's phone number (if it's a direct message)
        public string? GroupID { get; set; } // Group ID (if it's a group message)
        public string Content { get; set; } // Message text
        public string ContentType { get; set; } = "Text";
        public DateTime SentAt { get; set; } = DateTime.UtcNow; // Timestamp
        public bool Delivered { get; set; } = false; // Message delivery status
        public bool Read { get; set; } = false; // Message read status
    }
}