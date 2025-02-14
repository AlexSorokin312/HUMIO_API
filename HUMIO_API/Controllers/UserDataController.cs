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
        try
        {
            var userDto = await _userDataService.GetUserByIdAsync(userId);
            return Ok(userDto);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Получает данные пользователя по email.
    /// </summary>
    [HttpGet("email/{email}")]
    public async Task<IActionResult> GetUserByEmail(string email)
    {
        try
        {
            var userDto = await _userDataService.GetUserByEmailAsync(email);
            return Ok(userDto);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Получает список устройств, привязанных к пользователю.
    /// </summary>
    [HttpGet("{userId}/devices")]
    public async Task<IActionResult> GetUserDevices(string userId)
    {
        try
        {
            var devices = await _userDataService.GetUserDevicesAsync(userId);
            return Ok(devices);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Записывает покупку и одновременно обновляет дату окончания подписки.
    /// Принимает PurchaseRequest, который включает цену покупки, дату покупки и новую дату окончания подписки.
    /// </summary>
    /// <summary>
    /// Записывает покупку и одновременно обновляет дату окончания подписки.
    /// Принимает PurchaseRequest, который включает цену покупки, дату покупки и новую дату окончания подписки.
    /// </summary>
    [HttpPost("purchase")]
    public async Task<IActionResult> RecordPurchase([FromBody] PurchaseRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                Log.Warning("Unauthorized access attempt in RecordPurchase");
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
            Log.Error(ex, "Error in RecordPurchase for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
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
            Log.Error(ex, "Ошибка при проверке существования пользователя с email {Email}.", email);
            return BadRequest(new CommonResponse
            {
                Success = false,
                Message = ex.Message
            });
        }
    }
}

/// <summary>
/// Модель запроса для обновления даты окончания подписки.
/// </summary>
public class UpdateSubscriptionRequest
{
    public string UserId { get; set; }
    public DateTime SubscriptionEndDate { get; set; }
}