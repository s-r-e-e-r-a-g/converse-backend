using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Converse.Services.User;
using Converse.Models;
using BCrypt.Net;

namespace Converse.Services.User
{
    public class AuthenticationService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManagementService _userManagementService;

        public AuthenticationService(IConfiguration configuration, UserManagementService userManagementService)
        {
            _configuration = configuration;
            _userManagementService = userManagementService;
        }

        // Validate user credentials (use password verification if applicable)
        public async Task<bool> ValidateUser(string phoneNumber, string password, string publicKey, bool login)
        {
            var user = _userManagementService.GetUser(phoneNumber);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
                return false; // User not found
            if (login)
            {
                user.PublicKey = publicKey;
                _userManagementService.UpdateUserKey(user.PhoneNumber, user.PublicKey);
            }
            return true;
        }   

        // Generates JWT token for authentication
        public async Task<string?> GenerateJwtToken(string phoneNumber)
        {
            var user = _userManagementService.GetUser(phoneNumber);
            if (user == null)
            {
                return null; // Prevent token generation for non-existent users
            }

            var secretKey = _configuration["Jwt:SecretKey"];
            if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
            {
                throw new InvalidOperationException("JWT Secret Key must be at least 32 characters long.");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.PhoneNumber),
                new Claim("UserId", user.Id) // Assuming user has a unique ID
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMonths(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static string? GetUserIdFromRequest(HttpContext httpContext)
        {
            if (httpContext == null || !httpContext.Request.Headers.ContainsKey("Authorization"))
                return null;

            string authHeader = httpContext.Request.Headers["Authorization"];
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;

            string token = authHeader.Substring("Bearer ".Length).Trim();
            return GetUserIdFromToken(token);
        }

        private static string? GetUserIdFromToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var handler = new JwtSecurityTokenHandler();
            
            try
            {
                var jwtToken = handler.ReadJwtToken(token);
                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "UserId");

                return userIdClaim?.Value;
            }
            catch
            {
                return null;
            }
        }
    }
}