using System.ComponentModel.DataAnnotations;

namespace Humio.Requests
{
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string DeviceIdentifier { get; set; }
    }
}
