using Converse.Models;
using Converse.Data;

namespace Converse.Services.User
{
    public class UserManagementService
    {
        private readonly UserDb _userDb;

        public UserManagementService(UserDb userDb)
        {
            _userDb = userDb;
        }

        // Retrieve a user by phone number
        public UserData GetUser(string phoneNumber)
        {
            var user = _userDb.GetUser(phoneNumber);
            if (user != null) return user;
            return null;  // User not found
        }

        public string GetUserPhone(string userId)
        {
            var user = _userDb.GetUserPhone(userId);
            if (user != null) return user.PhoneNumber;
            return null;  // User not found
        }

        public List<string> GetUserGroups(string phoneNumber)
        {
            var user = _userDb.GetUser(phoneNumber);
            if (user != null) return user.JoinedGroups;
            return [];  // User not found
        }

        public bool UpdateUser(string phoneNumber, string newName)
        {
            if (_userDb.UpdateUserDetails(phoneNumber, newName)) return true;
            return false;
        }

        public bool UpdateUserKey(string phoneNumber, string publicKey)
        {
            if (_userDb.UpdateUserDetails(phoneNumber, publicKey)) return true;
            return false;
        }

        public bool UpdateUserPassword(string phoneNumber, string newPassword)
        {
            if (_userDb.UpdateUserPassword(phoneNumber, newPassword)) return true;
            return false;
        }

        public bool UpdateProfilePic(string phoneNumber, string profilePicUrl)
        {
            if (_userDb.UpdateProfilePic(phoneNumber, profilePicUrl)) return true;
            return false;
        }

        public bool RemoveUser(string phoneNumber)
        {
            if (_userDb.RemoveUser(phoneNumber)) return true;
            return false;
        }

        public bool JoinAGroup(string phoneNumber, string groupId)
        {
            if(_userDb.JoinAGroup(phoneNumber, groupId)) return true;
            return false;
        }

        public bool LeaveAGroup(string phoneNumber, string groupId)
        {
            if(_userDb.LeaveAGroup(phoneNumber, groupId)) return true;
            return false;
        }
    }
}