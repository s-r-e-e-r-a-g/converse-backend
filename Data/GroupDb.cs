using MongoDB.Driver;
using Converse.Models;
using ZstdSharp.Unsafe;

namespace Converse.Data
{
    public class GroupDb
    {
        private readonly IMongoCollection<GroupData> _groups;

        public GroupDb(IMongoDatabase database)
        {
            try
            {
                _groups = database.GetCollection<GroupData>("groups");
                Console.WriteLine("Successfully connected to MongoDB.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("MongoDB connection error: ", ex.Message);
            }
        }

        public List<GroupData> GetAllGroups() => _groups.Find(_ => true).ToList();

        public GroupData GetGroupById(string groupId) => 
            _groups.Find(g => g.GroupId == groupId).FirstOrDefault();

        public bool AddGroup(GroupData group)
        {
            try{
                _groups.InsertOne(group);
                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }

        public void UpdateGroup(string groupId, GroupData updatedGroup) =>
            _groups.ReplaceOne(g => g.GroupId == groupId, updatedGroup);

        public void DeleteGroup(string groupId) => _groups.DeleteOne(g => g.GroupId == groupId);
    }
}