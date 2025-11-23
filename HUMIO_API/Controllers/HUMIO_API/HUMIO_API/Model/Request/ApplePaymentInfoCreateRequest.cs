using System.ComponentModel.DataAnnotations;

namespace HUMIO_API.Requests
{
    public class ApplePaymentInfoCreateRequest
    {
        [Required]
        public string DeviceId { get; set; }

        public string Country { get; set; }

        [Range(0, int.MaxValue)]
        public int PaymentCount { get; set; }

        public DateTime? SubscriptionEndDate { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal Revenue { get; set; }
    }
}
