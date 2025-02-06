using System.ComponentModel.DataAnnotations;
namespace Humio.Requests
{
    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string? Password { get; set; }

        [Required]
        public string UserName { get; set; }

        public string Country { get; set; }

        public string Role { get; set; } = "User";
        public string DeviceIdentifier { get; set; }
        public string Platform { get; set; }
    }
}