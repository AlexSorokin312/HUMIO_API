using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HUMIO_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PromoCodeController : ControllerBase
    {
        private readonly IPromoCodeService _promoCodeService;

        public PromoCodeController(IPromoCodeService promoCodeService)
        {
            _promoCodeService = promoCodeService;
        }

        /// <summary>
        /// Применяет промокод для пользователя.
        /// Если промокод временный – обновляет TrialEndDate в UserData и удаляет промокод.
        /// Если постоянный – проверяет, не использовал ли пользователь этот код, обновляет TrialEndDate и регистрирует использование.
        /// </summary>
        /// <param name="request">Объект запроса с данными для применения промокода.</param>
        /// <returns>Результат операции.</returns>
        [HttpPost("apply")]
        [Authorize] // Применяем авторизацию по токену
        public async Task<IActionResult> ApplyPromoCode([FromBody] ApplyPromoCodeRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Извлекаем UserId из токена
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authorized" });
            }

            // Обновляем объект запроса, добавляя UserId
            request.UserId = userId;

            var response = await _promoCodeService.ApplyPromoCodeAsync(request);

            if (response.Success)
                return Ok(response);
            else
                return BadRequest(response);
        }
    }
}
