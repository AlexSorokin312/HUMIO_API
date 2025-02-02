using HUMIO_API.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HUMIO_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager;

        public UserController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// ✅ Получить пользователя по email
        /// </summary>
        [HttpGet("by-email/{email}")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"Пользователь с email {email} не найден.");
            }

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.Country
            });
        }

        /// <summary>
        /// ✅ Получить всех администраторов
        /// </summary>
        [HttpGet("admins")]
        public async Task<IActionResult> GetAllAdmins()
        {
            var users = await _userManager.GetUsersInRoleAsync("Admin");
            if (users == null || !users.Any())
            {
                return NotFound("Администраторы не найдены.");
            }

            return Ok(users.Select(user => new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.Country
            }));
        }
    }
}
