using HUMIO_API.Requests;
using HUMIO_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace HUMIO_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApplePaymentInfoController : ControllerBase
    {
        private readonly IApplePaymentInfoService _service;

        public ApplePaymentInfoController(IApplePaymentInfoService service)
        {
            _service = service;
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
                var created = await _service.CreateAsync(entity);
                return Ok(created);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
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
                var updated = await _service.UpdateAsync(deviceId, updatedEntity);
                return Ok(updated);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "ApplePaymentInfo not found for device" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
