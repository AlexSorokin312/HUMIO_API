using HUMIO_API.DBContext;
using HUMIO_API.Model.Request;
using HUMIO_API.Requests;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HUMIO_API.Services
{
    public interface IPasswordResetService
    {
        Task<CommonResponse> CreateResetCodeAsync(CreatePasswordResetRequest request);
        Task<CommonResponse> ResetPasswordAsync(PasswordResetRequest request);
    }

    public class PasswordResetService : IPasswordResetService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public PasswordResetService(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Сохраняет код восстановления, полученный от клиента, в базе данных.
        /// Срок действия кода — 5 минут.
        /// </summary>
        public async Task<CommonResponse> CreateResetCodeAsync(CreatePasswordResetRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new CommonResponse { Success = false, Message = "Пользователь не найден." };
            }

            var existingResets = _context.PasswordResets.Where(pr => pr.UserId == user.Id);
            if (existingResets.Any())
            {
                _context.PasswordResets.RemoveRange(existingResets);
            }

            var passwordReset = new PasswordReset
            {
                ResetCode = request.ResetCode,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            };

            await _context.PasswordResets.AddAsync(passwordReset);
            await _context.SaveChangesAsync();

            return new CommonResponse { Success = true, Message = "Код для восстановления успешно сохранён." };
        }

        public async Task<CommonResponse> ResetPasswordAsync(PasswordResetRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new CommonResponse { Success = false, Message = "Пользователь не найден." };
            }

            var passwordReset = await _context.PasswordResets
                .FirstOrDefaultAsync(pr => pr.UserId == user.Id && pr.ResetCode == request.ResetCode);

            if (passwordReset == null)
            {
                return new CommonResponse { Success = false, Message = "Неверный код восстановления." };
            }

            if (passwordReset.ExpiresAt < DateTime.UtcNow)
            {
                return new CommonResponse { Success = false, Message = "Код восстановления истёк." };
            }

            user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, request.NewPassword);
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new CommonResponse { Success = false, Message = "Ошибка обновления пароля: " + errors };
            }

            _context.PasswordResets.Remove(passwordReset);
            await _context.SaveChangesAsync();

            return new CommonResponse { Success = true, Message = "Пароль успешно обновлён." };
        }
    }
}
