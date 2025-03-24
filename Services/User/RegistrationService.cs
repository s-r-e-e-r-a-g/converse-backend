using Converse.Models;
using Converse.Data;
using System.Threading.Tasks;
using BCrypt.Net;

namespace Converse.Services.User
{
    public class RegistrationService
    {
        private readonly UserDb _userDb;

        public RegistrationService(UserDb userDb)
        {
            _userDb = userDb;
        }

        // Register a user with phone number
        public async Task<bool> RegisterUserAsync(string phoneNumber, string name, string password, string publicKey)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(name))
                return false;

            // Check if the user already exists based on the phone number
            var existingUser = _userDb.GetUser(phoneNumber);
            if (existingUser != null)
                return false;  // User already exists

            // Create a new UserData object

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            var user = new UserData
            {
                PhoneNumber = phoneNumber,
                Name = name,
                Password = hashedPassword,
                PublicKey = publicKey
            };

            // Add the new user to the database
            _userDb.AddUser(user);

            return true;
        }
    }
}