using System.Security.Claims;
using HUMIO_API.Requests;
using Microsoft.AspNetCore.Mvc;
using Serilog;

[Route("api/[controller]")]
[ApiController]
public class UserDataController : ControllerBase
{
    private readonly IUserDataService _userDataService;

    public UserDataController(IUserDataService userDataService)
    {
        _userDataService = userDataService;
    }

    /// <summary>
    /// Получает данные пользователя по Id.
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUserById(string userId)
    {
        Log.Information("GetUserById called with userId: {UserId}", userId);
        try
        {
            var userDto = await _userDataService.GetUserByIdAsync(userId);
            Log.Information("GetUserById succeeded for userId: {UserId}", userId);
            return Ok(userDto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in GetUserById for userId: {UserId}", userId);
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Получает данные пользователя по email.
    /// </summary>
    [HttpGet("email/{email}")]
    public async Task<IActionResult> GetUserByEmail(string email)
    {
        Log.Information("GetUserByEmail called with email: {Email}", email);
        try
        {
            var userDto = await _userDataService.GetUserByEmailAsync(email);
            Log.Information("GetUserByEmail succeeded for email: {Email}", email);
            return Ok(userDto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in GetUserByEmail for email: {Email}", email);
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Получает список устройств, привязанных к пользователю.
    /// </summary>
    [HttpGet("{userId}/devices")]
    public async Task<IActionResult> GetUserDevices(string userId)
    {
        Log.Information("GetUserDevices called for userId: {UserId}", userId);
        try
        {
            var devices = await _userDataService.GetUserDevicesAsync(userId);
            Log.Information("GetUserDevices succeeded for userId: {UserId}. Device count: {Count}", userId, devices.Count);
            return Ok(devices);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in GetUserDevices for userId: {UserId}", userId);
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Записывает покупку и одновременно обновляет дату окончания подписки.
    /// Принимает PurchaseRequest, который включает цену покупки, дату покупки и новую дату окончания подписки.
    /// </summary>
    [HttpPost("purchase")]
    public async Task<IActionResult> RecordPurchase([FromBody] PurchaseRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Log.Information("RecordPurchase called for userId: {UserId} with request: {@Request}", userId, request);
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                Log.Warning("Unauthorized access attempt in RecordPurchase: missing userId");
                return Unauthorized(new CommonResponse
                {
                    Success = false,
                    Message = "Пользователь не авторизован."
                });
            }

            Log.Information("Recording purchase for user {UserId}", userId);
            var response = await _userDataService.RecordPurchaseAndUpdateSubscriptionAsync(userId, request);

            if (response.Success)
            {
                Log.Information("Purchase recorded successfully for user {UserId}", userId);
                return Ok(response);
            }
            else
            {
                Log.Warning("Failed to record purchase for user {UserId}: {Message}", userId, response.Message);
                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in RecordPurchase for user {UserId}", userId);
            return BadRequest(new CommonResponse
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Проверяет, существует ли пользователь с указанным email.
    /// Возвращает объект CommonResponse с полями Success и Message.
    /// </summary>
    [HttpGet("exists/{email}")]
    public async Task<IActionResult> UserExists(string email)
    {
        Log.Information("UserExists called for email: {Email}", email);
        try
        {
            bool exists = await _userDataService.UserExistsAsync(email);
            var response = new CommonResponse
            {
                Success = exists,
                Message = exists ? "Пользователь найден" : "Пользователь не найден"
            };
            Log.Information("UserExists: Пользователь с email {Email} {Status}.", email, exists ? "найден" : "не найден");
            return Ok(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при проверке существования пользователя с email {Email}", email);
            return BadRequest(new CommonResponse
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Обновляет дату окончания подписки пользователя по email.
    /// </summary>
    /// <param name="request">Модель запроса, содержащая email пользователя и новую дату окончания подписки.</param>
    [HttpPut("update-subscription")]
    public async Task<IActionResult> UpdateSubscription([FromBody] UpdateSubscriptionRequestByEmail request)
    {
        Log.Information("UpdateSubscription called for email: {Email} with new subscription end date: {SubscriptionEndDate}", request.Email, request.SubscriptionEndDate);
        try
        {
            var response = await _userDataService.UpdateSubscriptionEndDateByEmailAsync(request.Email, request.SubscriptionEndDate);
            if (response.Success)
            {
                Log.Information("Subscription end date updated successfully for email {Email}", request.Email);
                return Ok(response);
            }
            else
            {
                Log.Warning("Failed to update subscription for email {Email}: {Message}", request.Email, response.Message);
                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating subscription for email {Email}", request.Email);
            return BadRequest(new CommonResponse
            {
                Success = false,
                Message = ex.Message
            });
        }
    }
}
