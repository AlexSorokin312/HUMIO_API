namespace HUMIO_API.Requests
{
    public class PurchaseRequest
    {
        /// <summary>
        /// Идентификатор пользователя, совершающего покупку.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Цена покупки.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Дата и время покупки.
        /// </summary>
        public DateTime PurchaseDate { get; set; }

        /// <summary>
        /// Новая дата окончания подписки.
        /// </summary>
        public DateTime SubscriptionEndDate { get; set; }
    }
}
