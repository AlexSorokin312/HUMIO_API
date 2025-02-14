namespace Humio.Requests
{
    public class PremiumResponse
    {
        public string UserId { get; set; }
        public DateTime SubscriptionEndDate { get; set; }
        public decimal CurrentPurchaseCost { get; set; }
        public string Message { get; set; }
    }
}