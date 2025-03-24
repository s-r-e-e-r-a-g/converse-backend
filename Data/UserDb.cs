using MongoDB.Driver;
using Converse.Models;

namespace Converse.Data
{
    public class UserDb
    {
        private readonly IMongoCollection<UserData> _users;
        public UserDb(IMongoDatabase database)
        {
            try
            {
                _users = database.GetCollection<UserData>("users");
                Console.WriteLine("Successfully connected to MongoDB.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("MongoDB connection error: ", ex.Message);
            }
        }

        public List<UserData> GetAllUsers() => _users.Find(_ => true).ToList();

        public UserData GetUserPhone(string userId) => _users.Find(user => user.Id == userId).FirstOrDefault();
        public UserData GetUser(string phoneNumber) => _users.Find(user => user.PhoneNumber == phoneNumber).FirstOrDefault();

        public bool RemoveUser(string phoneNumber)
        {
            var result = _users.DeleteOne(user => user.PhoneNumber == phoneNumber);
            if (result.IsAcknowledged) if (result.DeletedCount > 0) return true;
            return false;
        }

        public bool AddUser(UserData user)
        {
            try
            {
                _users.InsertOne(user);
                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }

        public bool UpdateUserDetails(string phoneNumber, string newName)
        {
            var filter = Builders<UserData>.Filter.Eq(user => user.PhoneNumber, phoneNumber);

            var updateDetails = Builders<UserData>.Update
            .Set(user => user.Name, newName);

            var updateResult = _users.UpdateOne(filter, updateDetails);

            if (updateResult.ModifiedCount > 0) return true;
            return false;
        }

        public bool UpdateUserKey(string phoneNumber, string publicKey)
        {
            var filter = Builders<UserData>.Filter.Eq(user => user.PhoneNumber, phoneNumber);

            var updateDetails = Builders<UserData>.Update
            .Set(user => user.PublicKey, publicKey);

            var updateResult = _users.UpdateOne(filter, updateDetails);

            if (updateResult.ModifiedCount > 0) return true;
            return false;
        }

        public bool UpdateUserPassword(string phoneNumber, string password)
        {
            var filter = Builders<UserData>.Filter.Eq(user => user.PhoneNumber, phoneNumber);

            var updateDetails = Builders<UserData>.Update
            .Set(user => user.Password, password);

            var updateResult = _users.UpdateOne(filter, updateDetails);

            if (updateResult.ModifiedCount > 0) return true;
            return false;
        }

        public bool UpdateProfilePic(string phoneNumber, string profilePicUrl)
        {
            var filter = Builders<UserData>.Filter.Eq(user => user.PhoneNumber, phoneNumber);

            var updateDetails = Builders<UserData>.Update
            .Set(user => user.ProfilePicUrl, profilePicUrl);

            var updateResult = _users.UpdateOne(filter, updateDetails);

            if (updateResult.ModifiedCount > 0) return true;
            return false;
        }

        public bool JoinAGroup(string phoneNumber, string groupId)
        {
            var filter = Builders<UserData>.Filter.Eq(user => user.PhoneNumber, phoneNumber);

            var updateDetails = Builders<UserData>.Update
            .AddToSet(user => user.JoinedGroups, groupId);

            var updateResult = _users.UpdateOne(filter, updateDetails);

            if (updateResult.ModifiedCount > 0) return true;
            return false;
        }

        public bool LeaveAGroup(string phoneNumber, string groupId)
        {
            var filter = Builders<UserData>.Filter.Eq(user => user.PhoneNumber, phoneNumber);

            var updateDetails = Builders<UserData>.Update
            .Pull(user => user.JoinedGroups, groupId);

            var updateResult = _users.UpdateOne(filter, updateDetails);

            if (updateResult.ModifiedCount > 0) return true;
            return false;
        }
    }
}