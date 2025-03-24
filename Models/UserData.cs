using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Converse.Models
{
    public class UserData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; } // MongoDB identifier
        public string PhoneNumber { get; set; } // User's phone number (used as ID)
        public string Name { get; set; } // Display name
        public string Password { get; set; }
        public string PublicKey { get; set; }
        public string ProfilePicUrl { get; set; } = "";
        public List<string> JoinedGroups { get; set; } = [];
    }
}