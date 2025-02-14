public class UpdateSubscriptionRequestByEmail
{
    /// <summary>
    /// Email пользователя, для которого нужно обновить дату окончания подписки.
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// Новая дата окончания подписки.
    /// </summary>
    public DateTime SubscriptionEndDate { get; set; }
}