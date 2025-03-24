using Converse.Models;
using Converse.Services.User;
using Converse.Services.Group;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Converse.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GroupController : ControllerBase
    {
        private readonly GroupManagementService _groupManagementService;
        private readonly UserManagementService _userManagementService;

        public GroupController(GroupManagementService groupManagementService, UserManagementService userManagementService)
        {
            _groupManagementService = groupManagementService;
            _userManagementService = userManagementService;
        }

        [HttpPost("create")]
        public IActionResult CreateGroup([FromBody] GroupCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = AuthenticationService.GetUserIdFromRequest(HttpContext);
            if (userId == null) return Unauthorized("Invalid Token");
            
            var userPhone = _userManagementService.GetUserPhone(userId);

            if (!string.IsNullOrEmpty(request.GroupName) && !string.IsNullOrEmpty(userId))
            {
                var group = new GroupData(request.GroupName, userPhone, request.privateHosting);

                if (_groupManagementService.CreateGroup(group))
                    return CreatedAtAction(nameof(GetGroup), new { groupId = group.GroupId }, group);

                return Conflict(new { message = $"Group '{group.GroupId}' already exists." });
            }
            return BadRequest(new { message = "Error in Group Name" });
        }

        [HttpGet("{groupId}")]
        public IActionResult GetGroup(string groupId)
        {
            var group = _groupManagementService.GetGroupById(groupId);
            return group != null ? Ok(group) : NotFound(new { message = $"Group '{groupId}' not found." });
        }

        [HttpDelete("{groupId}")]
        public IActionResult DeleteGroup(string groupId)
        {
            if (_groupManagementService.DeleteGroup(groupId))
                return Ok(new { message = $"Group '{groupId}' removed." });

            return NotFound(new { message = $"No such group '{groupId}' exists." });
        }

        [HttpPost("members")]
        public IActionResult AddMembers([FromBody] AddToGroupRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            var userId = AuthenticationService.GetUserIdFromRequest(HttpContext);
            if (userId == null) return Unauthorized("Invalid Token");
            
            var userPhone = _userManagementService.GetUserPhone(userId);
            if (userPhone == null || !_groupManagementService.GetGroupAdmins(request.GroupId).Contains(userPhone))
            {
                return Unauthorized("No Admin Prvilege.");
            }

            List<string> message = [];

            foreach (var newMember in request.PhoneNumbers)
            {
                var user =_userManagementService.GetUser(newMember);
                if (user != null)
                {
                    if (_userManagementService.JoinAGroup(user.PhoneNumber, request.GroupId)){
                        if (_groupManagementService.AddMemberToGroup(request.GroupId, user.PhoneNumber))
                        {
                            message.Add(user.PhoneNumber);
                        }
                        else{
                            _userManagementService.LeaveAGroup(user.PhoneNumber, request.GroupId);
                        }
                    }
                }
            }
            return Ok(new { added = string.Join(" ", message) });
        }

        [HttpDelete("members")]
        public IActionResult RemoveMember([FromBody] GroupRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = AuthenticationService.GetUserIdFromRequest(HttpContext);
            if (userId == null) return Unauthorized("Invalid Token");
            
            var userPhone = _userManagementService.GetUserPhone(userId);
            if (userPhone == null || !_groupManagementService.GetGroupAdmins(request.GroupId).Contains(userPhone))
            {
                return Unauthorized("No Admin Prvilege.");
            }

            _groupManagementService.RemoveAdminPrivilege(request.GroupId, request.PhoneNumber);
            if (_groupManagementService.RemoveMemberFromGroup(request.GroupId, request.PhoneNumber))
                return Ok(new { message = $"Member '{request.PhoneNumber}' removed from group '{request.GroupId}'." });

            return NotFound(new { message = $"Member '{request.PhoneNumber}' not found in group '{request.GroupId}'." });
        }

        [HttpPost("admins")]
        public IActionResult MakeAdmin([FromBody] GroupRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            var userId = AuthenticationService.GetUserIdFromRequest(HttpContext);
            if (userId == null) return Unauthorized("Invalid Token");
            
            var userPhone = _userManagementService.GetUserPhone(userId);
            if (userPhone == null || !_groupManagementService.GetGroupAdmins(request.GroupId).Contains(userPhone))
            {
                return Unauthorized("No Admin Prvilege.");
            }
            
            if (_groupManagementService.MakeAdmin(request.GroupId, request.PhoneNumber))
                return Ok(new { message = $"Member '{request.PhoneNumber}' added as admin '{request.GroupId}'." });

            return NotFound(new { message = $"Member '{request.PhoneNumber}' not found in group '{request.GroupId}'." });
        }

        [HttpDelete("admins")]
        public IActionResult RemoveAdmin([FromBody] GroupRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            var userId = AuthenticationService.GetUserIdFromRequest(HttpContext);
            if (userId == null) return Unauthorized("Invalid Token");
            
            var userPhone = _userManagementService.GetUserPhone(userId);
            if (userPhone == null || !_groupManagementService.GetGroupAdmins(request.GroupId).Contains(userPhone))
            {
                return Unauthorized("No Admin Prvilege.");
            }
            
            if (_groupManagementService.RemoveAdminPrivilege(request.GroupId, request.PhoneNumber))
                return Ok(new { message = $"Member '{request.PhoneNumber}' removed from admin position '{request.GroupId}'." });

            return NotFound(new { message = $"Member '{request.PhoneNumber}' not found in group '{request.GroupId}'." });
        }

        [HttpDelete("leave")]
        public IActionResult LeaveGroup([FromBody] GroupRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            var userId = AuthenticationService.GetUserIdFromRequest(HttpContext);
            if (userId == null) return Unauthorized("Invalid Token");
            
            var userPhone = _userManagementService.GetUserPhone(userId);
            if (userPhone == null || !_groupManagementService.GetGroupAdmins(request.GroupId).Contains(userPhone))
            {
                return Unauthorized("No Admin Prvilege.");
            }
            
            _groupManagementService.RemoveAdminPrivilege(request.GroupId, request.PhoneNumber);
            if (_groupManagementService.RemoveMemberFromGroup(request.GroupId, request.PhoneNumber))
                return Ok(new { message = $"Member '{request.PhoneNumber}' left from group '{request.GroupId}'." });

            return NotFound(new { message = $"Member '{request.PhoneNumber}' not found in group '{request.GroupId}'." });
        }

        [HttpGet("{groupId}/members")]
        public IActionResult GetGroupMembers(string groupId)
        {
            var members = _groupManagementService.GetGroupMembers(groupId);
            return members.Count != 0 ? Ok(members) : NotFound(new { message = $"No members found for group '{groupId}'." });
        }

        [HttpGet("{groupId}/admins")]
        public IActionResult GetGroupAdmins(string groupId)
        {
            var admins = _groupManagementService.GetGroupAdmins(groupId);
            return admins.Count != 0 ? Ok(admins) : NotFound(new { message = $"No admins found for group '{groupId}'." });
        }
    }

    public class AddToGroupRequest
    {
        [Required]
        public string GroupId { get; set; }

        [Required]
        public List<string> PhoneNumbers { get; set; }
    }

    public class GroupRequest
    {
        [Required]
        public string GroupId { get; set; }

        [Required]
        public string PhoneNumber { get; set; }
    }

    // Define the request model
    public class GroupCreateRequest
    {
        [Required]
        public string GroupName { get; set; }
        public bool privateHosting { get; set;} = false;
    }
}