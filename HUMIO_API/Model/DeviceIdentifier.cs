using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HUMIO_API.Model
{
    public class DeviceIdentifier
    {
        [Key] // ✅ Id как PK
        public int Id { get; set; }

        [Required]
        public string Identifier { get; set; }

        [Required]
        [ForeignKey("User")] // ✅ Внешний ключ
        public string UserId { get; set; }

        public User User { get; set; }
    }
}
