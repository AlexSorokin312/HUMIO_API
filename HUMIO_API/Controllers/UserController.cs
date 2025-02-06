using HUMIO_API.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HUMIO_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] 
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager;

        public UserController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Получить пользователя по email (только для аутентифицированных)
        /// </summary>
        [HttpGet("by-email/{email}")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"Пользователь с email {email} не найден.");
            }

            var response = new UserResponse
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
            };

            return Ok(response);
        }

        /// <summary>
        /// Получить пользователя по id (только для аутентифицированных)
        /// </summary>
        [HttpGet("by-id/{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound($"Пользователь с id {id} не найден.");
            }

            var response = new UserResponse
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
            };

            return Ok(response);
        }

        /// <summary>
        /// Получить всех пользователей из заданной страны (только для аутентифицированных)
        /// </summary>
        [HttpGet("by-country/{country}")]
        public IActionResult GetUsersByCountry(string country)
        {

            return null;
        }

        /// <summary>
        /// Получить всех администраторов (только для админов)
        /// </summary>
        [HttpGet("admins")]
        [Authorize(Roles = "Admin")] // Только админы могут вызвать этот метод
        public async Task<IActionResult> GetAllAdmins()
        {
            var users = await _userManager.GetUsersInRoleAsync("Admin");
            if (users == null || !users.Any())
            {
                return NotFound("Администраторы не найдены.");
            }

            var response = users.Select(user => new UserResponse
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
            });

            return Ok(response);
        }
    }
}
