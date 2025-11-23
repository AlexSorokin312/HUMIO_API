using HUMIO_API.Requests;
using HUMIO_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HUMIO_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApplePaymentInfoController : ControllerBase
    {
        private readonly IApplePaymentInfoService _service;
        private readonly ILogger<ApplePaymentInfoController> _logger;

        public ApplePaymentInfoController(IApplePaymentInfoService service, ILogger<ApplePaymentInfoController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("{deviceId}")]
        public async Task<IActionResult> Get(string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return BadRequest(new { message = "DeviceId is required" });
            }

            _logger.LogInformation("Fetching ApplePaymentInfo for device {DeviceId}", deviceId);
            var info = await _service.GetByDeviceIdAsync(deviceId);
            if (info == null)
            {
                _logger.LogWarning("ApplePaymentInfo not found for device {DeviceId}", deviceId);
                return NotFound(new { message = "ApplePaymentInfo not found for device" });
            }

            return Ok(info);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] ApplePaymentInfoCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entity = new ApplePaymentInfo
            {
                DeviceId = request.DeviceId,
                Country = request.Country,
                PaymentCount = request.PaymentCount,
                SubscriptionEndDate = request.SubscriptionEndDate,
                Revenue = request.Revenue
            };

            try
            {
                _logger.LogInformation("Creating ApplePaymentInfo for device {DeviceId}", request.DeviceId);
                var created = await _service.CreateAsync(entity);
                return Ok(created);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Device not found for ApplePaymentInfo create, deviceId={DeviceId}", request.DeviceId);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "ApplePaymentInfo already exists for deviceId={DeviceId}", request.DeviceId);
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{deviceId}")]
        public async Task<IActionResult> Update(string deviceId, [FromBody] ApplePaymentInfoUpdateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedEntity = new ApplePaymentInfo
            {
                DeviceId = deviceId,
                Country = request.Country,
                PaymentCount = request.PaymentCount,
                SubscriptionEndDate = request.SubscriptionEndDate,
                Revenue = request.Revenue
            };

            try
            {
                _logger.LogInformation("Updating ApplePaymentInfo for device {DeviceId}", deviceId);
                var updated = await _service.UpdateAsync(deviceId, updatedEntity);
                return Ok(updated);
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning("ApplePaymentInfo not found for device {DeviceId}", deviceId);
                return NotFound(new { message = "ApplePaymentInfo not found for device" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Device not found while updating ApplePaymentInfo, deviceId={DeviceId}", deviceId);
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{deviceId}")]
        public async Task<IActionResult> Delete(string deviceId)
        {
            _logger.LogInformation("Deleting ApplePaymentInfo for device {DeviceId}", deviceId);
            var deleted = await _service.DeleteAsync(deviceId);
            if (!deleted)
            {
                _logger.LogWarning("ApplePaymentInfo not found for delete, deviceId={DeviceId}", deviceId);
                return NotFound(new { message = "ApplePaymentInfo not found for device" });
            }

            return NoContent();
        }
    }
}
