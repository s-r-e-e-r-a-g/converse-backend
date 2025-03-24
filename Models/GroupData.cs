using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Converse.Models
{
    public class GroupData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? GroupId { get; set; } // MongoDB will use this as the unique identifier
        public string GroupName { get; set; }
        public string GroupCreator { get; set; }
        public List<string> Members { get; set; } // List of user phone numbers
        public List<string> Admins { get; set; }
        public bool IsPrivateHosted = false;

        public GroupData(string groupName, string creator, bool privateHosting)
        {
            GroupName = groupName;
            GroupCreator = creator;
            Members = new List<string> { creator };
            Admins = new List<string> { creator };
            IsPrivateHosted = privateHosting;
        }
    }
}