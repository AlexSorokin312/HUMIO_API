using Microsoft.AspNetCore.Mvc;
using HUMIO_API.Requests;
using System;
using System.Threading.Tasks;

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
    /// Модель запроса для обновления даты окончания подписки.
    /// </summary>
    public class UpdateSubscriptionRequest
    {
        public string UserId { get; set; }
        public DateTime SubscriptionEndDate { get; set; }
    }

    /// <summary>
    /// Записывает покупку и одновременно обновляет дату окончания подписки.
    /// Принимает PurchaseRequest, который включает цену покупки, дату покупки и новую дату окончания подписки.
    /// </summary>
    [HttpPost("purchase")]
    public async Task<IActionResult> RecordPurchase([FromBody] PurchaseRequest request)
    {
        try
        {
            await _userDataService.RecordPurchaseAndUpdateSubscriptionAsync(request);
            return Ok(new { message = "Purchase recorded and subscription updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
