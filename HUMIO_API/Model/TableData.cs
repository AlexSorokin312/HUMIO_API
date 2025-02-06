using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace HUMIO_API.Requests
{
    public class User : IdentityUser
    {
        public ICollection<UserDevice> UserDevices { get; set; } = new List<UserDevice>();
        public string? GoogleId { get; set; }
        public string Name { get; set; } 
        public UserData UserData { get; set; }
    }

    public class UserData
    {
        [Key, ForeignKey("User")]
        public string UserId { get; set; }

        public string Country { get; set; }
        public string Platform { get; set; }
        public int PaymentCount { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }

        public User User { get; set; }
    }

    public class DeviceIdentifier
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string DeviceId { get; set; }

        public DateTime? TrialEndDate { get; set; }

        public ICollection<UserDevice> UserDevices { get; set; } = new List<UserDevice>();
    }

    public class UserDevice
    {
        [Key]
        public int Id { get; set; }

        [Required, ForeignKey("User")]
        public string UserId { get; set; } // Должно быть string

        [Required, ForeignKey("DeviceIdentifier")]
        public int DeviceId { get; set; }

        public User User { get; set; }
        public DeviceIdentifier DeviceIdentifier { get; set; }
    }

    public class Purchase
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }
        [Required]
        public decimal Price { get; set; }

        [Required]
        public DateTime PurchaseDate { get; set; }
        [Required]
        public DateTime SubscriptionEndDate { get; set; }
    }
}
