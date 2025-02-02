using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace HUMIO_API.Model
{
    public class User : IdentityUser
    {
        public ICollection<DeviceIdentifier> DeviceIdentifiers { get; set; } = new List<DeviceIdentifier>();
        public string Country { get; set; }
        public Trial Trial { get; set; }
        public Premium Premium { get; set; }
        public UserStats UserStats { get; set; }
    }

    // Таблица пробного периода
    public class Trial
    {
        [Key] // ✅ TrialId как PK
        public int TrialId { get; set; }

        [Required]
        [ForeignKey("User")] // ✅ Внешний ключ
        public string UserId { get; set; }

        public DateTime TrialEndDate { get; set; }

        public User User { get; set; }
    }

    // Таблица премиум-подписки
    public class Premium
    {
        [Key] // ✅ PremiumId как PK
        public int PremiumId { get; set; }

        [Required]
        [ForeignKey("User")] // ✅ Внешний ключ
        public string UserId { get; set; }

        public DateTime SubscriptionEndDate { get; set; }
        public decimal CurrentPurchaseCost { get; set; }

        public User User { get; set; }
    }

    // Таблица статистики пользователя
    public class UserStats
    {
        [Key] // ✅ UserStatsId как PK
        public int UserStatsId { get; set; }

        [Required]
        [ForeignKey("User")] // ✅ Внешний ключ
        public string UserId { get; set; }

        public TimeSpan TimeSpentInApp { get; set; }
        public decimal TotalPurchaseAmount { get; set; }
        public int PurchaseCount { get; set; }

        public User User { get; set; }
    }
}
