namespace HUMIO_API.Requests
{
    public class PurchaseRequest
    {
        public decimal Price { get; set; }
        public DateTime PurchaseDate { get; set; }
        public DateTime SubscriptionEndDate { get; set; }
    }
}
