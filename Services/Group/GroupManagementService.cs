using Converse.Models;
using Converse.Data;

namespace Converse.Services.Group
{
    public class GroupManagementService
    {
        private readonly GroupDb _groupDb;

        public GroupManagementService(GroupDb groupDb)
        {
            _groupDb = groupDb;
        }

        public bool CreateGroup(GroupData group)
        {
            return _groupDb.AddGroup(group);
        }

        public GroupData GetGroupById(string groupId)
        {
            return _groupDb.GetGroupById(groupId);
        }

        public bool RemoveMemberFromGroup(string groupId, string phoneNumber)
        {
            var group = _groupDb.GetGroupById(groupId);
            if (group != null && group.Members.Contains(phoneNumber))
            {
                group.Members.Remove(phoneNumber);
                if (group.Members.Count == 0)
                {
                    _groupDb.DeleteGroup(groupId);
                    return true;
                }
                if (group.Admins.Count == 0) group.Admins.Add(group.Members.First());
                _groupDb.UpdateGroup(groupId, group);
                return true;
            }
            return false;
        }

        public bool AddMemberToGroup(string groupId, string phoneNumber)
        {
            var group = _groupDb.GetGroupById(groupId);
            if (group != null && !group.Members.Contains(phoneNumber))
            {
                group.Members.Add(phoneNumber);
                _groupDb.UpdateGroup(groupId, group);
                return true;
            }
            return false;
        }

        public bool MakeAdmin(string groupId, string phoneNumber)
        {
            var group = _groupDb.GetGroupById(groupId);
            if (group != null && group.Members.Contains(phoneNumber) && !group.Admins.Contains(phoneNumber))
            {
                group.Admins.Add(phoneNumber);
                _groupDb.UpdateGroup(groupId, group);
                return true;
            }
            return false;
        }

        public bool RemoveAdminPrivilege(string groupId, string phoneNumber)
        {
            var group = _groupDb.GetGroupById(groupId);
            if (group != null && group.Members.Contains(phoneNumber))
            {
                group.Admins.Remove(phoneNumber);
                if (group.Admins.Count == 0) group.Admins.Add(group.Members.First());
                _groupDb.UpdateGroup(groupId, group);
                return true;
            }
            return false;
        }

        public List<string> GetGroupMembers(string groupId)
        {
            var group = _groupDb.GetGroupById(groupId);
            return group?.Members ?? [];
        }

        public List<string> GetGroupAdmins(string groupId)
        {
            var group = _groupDb.GetGroupById(groupId);
            return group?.Admins ?? [];
        }

        public bool DeleteGroup(string groupId)
        {
            var group = _groupDb.GetGroupById(groupId);
            if (group != null)
            {
                _groupDb.DeleteGroup(groupId);
                return true;
            }
            return false;
        }
    }
}