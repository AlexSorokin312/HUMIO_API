using System.ComponentModel.DataAnnotations;

namespace Humio.Requests
{
    public class PremiumUpdateRequest
    {
        [Required]
        public string UserId { get; set; }

        /// <summary>
        /// Новая дата окончания подписки.
        /// </summary>
        [Required]
        public DateTime SubscriptionEndDate { get; set; }

        /// <summary>
        /// Текущая стоимость покупки (например, стоимость продления).
        /// </summary>
        [Required]
        public decimal CurrentPurchaseCost { get; set; }
    }
}