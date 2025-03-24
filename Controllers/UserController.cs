using Microsoft.AspNetCore.Mvc;
using Converse.Services.User;
using Converse.Models;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Azure;
using Microsoft.VisualBasic;

namespace Converse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly RegistrationService _registrationService;
        private readonly UserManagementService _userManagementService;
        private readonly AuthenticationService _authenticationService;

        public UserController(RegistrationService registrationService, UserManagementService userManagementService, AuthenticationService authenticationService)
        {
            _registrationService = registrationService;
            _userManagementService = userManagementService;
            _authenticationService = authenticationService;
        }

        [HttpGet("{phoneNumber}")]
        public IActionResult GetUser(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return BadRequest("Phone number is required.");

            var userId = AuthenticationService.GetUserIdFromRequest(HttpContext);
            var user = _userManagementService.GetUser(phoneNumber);
            if (user != null)
            {
                if (userId == user.Id) return Ok(user);
                return Ok( new { user.Id, user.Name, user.PhoneNumber, user.ProfilePicUrl, user.PublicKey, JoinedGroups = (List<string>?)null});
            }
            return NotFound("User not found");
        }

        [HttpPost("removeUser")]
        public async Task<IActionResult> RemoveUser([FromBody] RemoveRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Phone number and password are required.");
            
            var userId = AuthenticationService.GetUserIdFromRequest(HttpContext);
            if (userId != null && _userManagementService.GetUserPhone(userId) != request.PhoneNumber)
            {
                return Unauthorized("Phone number not validated.");
            }

            // Validate user credentials
            var isValidUser = await _authenticationService.ValidateUser(request.PhoneNumber, request.Password, null, false);
            if (!isValidUser)
                return Unauthorized("Invalid credentials.");

            return _userManagementService.RemoveUser(request.PhoneNumber)
                ? Ok(new { message = "User removed successfully." })
                : NotFound("User not found");
        }

        [HttpPut("updateUser")]
        public IActionResult UpdateUser([FromBody] UpdateUserRequest user)
        {
            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                return BadRequest("Phone number is required.");
            
            var userId = AuthenticationService.GetUserIdFromRequest(HttpContext);
            if (userId != null && _userManagementService.GetUserPhone(userId) != user.PhoneNumber)
            {
                return Unauthorized("Phone number not validated.");
            }

            var existingUser = _userManagementService.GetUser(user.PhoneNumber);
            if (existingUser == null)
                return NotFound("User not found.");

            //user.NewPhone ??= user.PhoneNumber;
            user.Name ??= existingUser.Name;

            bool result = _userManagementService.UpdateUser(user.PhoneNumber, user.Name);
            return result ? Ok("User updated successfully.") : StatusCode(500, "Update failed.");
        }

        [HttpPut("updatePassword")]
        public async Task<IActionResult> UpdateUserPassword([FromBody] UpdateUserPasswordRequest user)
        {
            if (string.IsNullOrWhiteSpace(user.PhoneNumber) || string.IsNullOrWhiteSpace(user.CurrentPassword) || string.IsNullOrWhiteSpace(user.NewPassword))
                return BadRequest("Phone number is required.");
            
            var userId = AuthenticationService.GetUserIdFromRequest(HttpContext);
            var existingUser = _userManagementService.GetUser(user.PhoneNumber);

            if (existingUser == null || existingUser.Id == null)
                return NotFound("User not found.");

            if (userId != null && userId != existingUser.Id)
            {
                return Unauthorized("Phone number not validated.");
            }            
            if (! await _authenticationService.ValidateUser(existingUser.PhoneNumber, user.CurrentPassword, null, false))
            {
                return Unauthorized("Wrong Password");
            }

            bool result = _userManagementService.UpdateUserPassword(user.PhoneNumber, user.NewPassword);
            return result ? Ok("User updated successfully.") : StatusCode(500, "Update failed.");
        }

        [HttpPut("updateProfilePic")]
        public IActionResult UpdateProfilePicUrl([FromBody] UpdateProfilePicRequest user)
        {
            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                return BadRequest("Phone number is required.");
            
            var userId = AuthenticationService.GetUserIdFromRequest(HttpContext);
            if (userId != null && _userManagementService.GetUserPhone(userId) != user.PhoneNumber)
            {
                return Unauthorized("Phone number not validated.");
            }
            
            var existingUser = _userManagementService.GetUser(user.PhoneNumber);
            if (existingUser == null || existingUser.Id == null)
                return NotFound("User not found.");

            bool result = _userManagementService.UpdateProfilePic(user.PhoneNumber, user.NewProfilePicUrl);
            return result ? Ok("User updated successfully.") : StatusCode(500, "Update failed.");
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserData request)
        {
            if (string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Phone number and name are required.");
            
            if(string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Password is Unacceptable.");

            var result = await _registrationService.RegisterUserAsync(request.PhoneNumber, request.Name, request.Password, request.PublicKey);
            if (!result)
                return BadRequest("User already exists or invalid input.");

            // Generate JWT token after successful registration
            var token = _authenticationService.GenerateJwtToken(request.PhoneNumber);

            return Ok(new { Message = "User registered successfully.", Token = token });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Authenticate([FromBody] AuthenticateRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.PublicKey))
                return Ok("Phone number and password are required.");

            // Validate user credentials
            var isValidUser = await _authenticationService.ValidateUser(request.PhoneNumber, request.Password, request.PublicKey, false);
            if (!isValidUser)
                return Ok("Invalid credentials.");

            var token = _authenticationService.GenerateJwtToken(request.PhoneNumber);
            var userPublicKey = _userManagementService.GetUser(request.PhoneNumber).PublicKey;
            return Ok(new { Token = token, PublicKey = userPublicKey});
        }

    }

    // DTO (Data Transfer Object) for updation request
    public class UpdateUserRequest
    {
        public string PhoneNumber { get; set; }
        public string? Name { get; set; }
        public string? NewPhone { get; set; }
    }

    public class UpdateUserPasswordRequest
    {
        public string PhoneNumber { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public class UpdateProfilePicRequest
    {
        public string PhoneNumber { get; set; }
        public string? NewProfilePicUrl { get; set; }
    }

    // DTO for login/authentication request
    public class AuthenticateRequest
    {
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public string PublicKey { get; set; }
    }

    public class RemoveRequest
    {
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
    }
}