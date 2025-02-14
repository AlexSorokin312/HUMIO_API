
/// <summary>
/// Модель запроса для обновления даты окончания подписки.
/// </summary>
public class UpdateSubscriptionRequest
{
    public string UserId { get; set; }
    public DateTime SubscriptionEndDate { get; set; }
}
