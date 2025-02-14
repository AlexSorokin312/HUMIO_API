public interface IPromoCodeService
{
    /// <summary>
    /// Применяет промокод для указанного пользователя и обновляет TrialEndDate в UserData.
    /// Если промокод временный – продлевает TrialEndDate и удаляет промокод.
    /// Если постоянный – проверяет, не использовал ли уже пользователь его, продлевает TrialEndDate и регистрирует использование.
    /// </summary>
    Task<CommonResponse> ApplyPromoCodeAsync(ApplyPromoCodeRequest request);
}