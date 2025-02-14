using HUMIO_API.Model.Request;
using HUMIO_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace HUMIO_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PasswordResetController : ControllerBase
    {
        private readonly IPasswordResetService _passwordResetService;

        public PasswordResetController(IPasswordResetService passwordResetService)
        {
            _passwordResetService = passwordResetService;
        }

        /// <summary>
        /// Принимает запрос с почтой и сгенерированным на клиенте кодом восстановления.
        /// Код также отправляется на почту пользователю (это делает клиент).
        /// </summary>
        [HttpPost("create-code")]
        public async Task<IActionResult> CreateResetCode([FromBody] CreatePasswordResetRequest request)
        {
            var response = await _passwordResetService.CreateResetCodeAsync(request);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// Принимает запрос на сброс пароля с почтой, кодом и новым паролем.
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordResetRequest request)
        {
            var response = await _passwordResetService.ResetPasswordAsync(request);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}
