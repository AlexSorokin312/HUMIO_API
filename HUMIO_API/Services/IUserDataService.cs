using HUMIO_API.Requests;

public interface IUserDataService
{
    /// <summary>
    /// Получает данные пользователя по Id (включая связанные UserData и устройства).
    /// </summary>
    Task<UserDto> GetUserByIdAsync(string userId);

    /// <summary>
    /// Получает данные пользователя по email (включая связанные UserData и устройства).
    /// </summary>
    Task<UserDto> GetUserByEmailAsync(string email);

    /// <summary>
    /// Получает список устройств (DeviceIdentifier), привязанных к пользователю.
    /// </summary>
    Task<List<DeviceIdentifier>> GetUserDevicesAsync(string userId);

    /// <summary>
    /// Обновляет дату окончания подписки для пользователя.
    /// </summary>
    Task<CommonResponse> RecordPurchaseAndUpdateSubscriptionAsync(string id, PurchaseRequest request);

    Task<bool> UserExistsAsync(string email);
}
