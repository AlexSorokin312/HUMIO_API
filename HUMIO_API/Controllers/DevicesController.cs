using HUMIO_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace HUMIO_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly IDeviceService _deviceService;

        public DeviceController(IDeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        /// <summary>
        /// Возвращает существующую запись DeviceIdentifier или создаёт новую,
        /// если записи с данным DeviceId ещё нет.
        /// </summary>
        /// <param name="request">Данные устройства.</param>
        /// <returns>Объект DeviceIdentifier.</returns>
        [HttpPost("create")]
        public async Task<IActionResult> GetOrCreateDevice([FromBody] DeviceRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var device = await _deviceService.GetOrCreateDeviceAsync(request);
                return Ok(device);
            }
            catch (Exception ex)
            {
                // Можно добавить логирование ошибки здесь
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Удаляет запись DeviceIdentifier, если она не связана с зарегистрированными пользователями.
        /// </summary>
        /// <param name="deviceId">Идентификатор устройства.</param>
        /// <returns>Результат операции.</returns>
        [HttpDelete("delete/{deviceId}")]
        public async Task<IActionResult> DeleteDevice(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId))
                return BadRequest("DeviceId is required");

            try
            {
                bool deleted = await _deviceService.DeleteAnonymousDeviceAsync(deviceId);
                if (deleted)
                {
                    return Ok(new { message = "Device deleted successfully." });
                }
                else
                {
                    return BadRequest(new { error = "Device not deleted. It might be associated with a registered user or does not exist." });
                }
            }
            catch (Exception ex)
            {
                // Можно добавить логирование ошибки здесь
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
