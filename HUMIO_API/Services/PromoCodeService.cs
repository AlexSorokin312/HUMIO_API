using HUMIO_API.DBContext;
using HUMIO_API.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HUMIO_API.Services
{
    public class PromoCodeService : IPromoCodeService
    {
        private readonly AppDbContext _context;

        public PromoCodeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CommonResponse> ApplyPromoCodeAsync(ApplyPromoCodeRequest request)
        {
            using (IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Загружаем пользователя с UserData
                    var user = await _context.Users
                        .Include(u => u.UserData)
                        .FirstOrDefaultAsync(u => u.Id == request.UserId);
                    if (user == null)
                    {
                        return new CommonResponse { Success = false, Message = "User not found." };
                    }

                    // Попытка применить временный промокод
                    var tempCode = await _context.TemporaryPromoCodes
                        .FirstOrDefaultAsync(pc => pc.Code == request.PromoCode);
                    if (tempCode != null)
                    {
                        var response = await ApplyTemporaryPromoCodeAsync(user, tempCode, transaction);
                        return response;
                    }
                    else
                    {
                        // Если временный промокод не найден, ищем постоянный промокод
                        var permCode = await _context.PermanentPromoCodes
                            .Include(pc => pc.PermanentPromoCodeUsages)
                            .FirstOrDefaultAsync(pc => pc.Code == request.PromoCode);
                        if (permCode == null)
                        {
                            await transaction.RollbackAsync();
                            return new CommonResponse { Success = false, Message = "Promo code not found." };
                        }

                        var response = await ApplyPermanentPromoCodeAsync(user, permCode, request.UserId, transaction);
                        return response;
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return new CommonResponse { Success = false, Message = $"Error applying promo code: {ex.Message}" };
                }
            }
        }

        /// <summary>
        /// Обновляет поле TrialEndDate в UserData, прибавляя указанное число дней.
        /// </summary>
        private void UpdateTrialEndDate(UserData userData, int extensionDays)
        {
            if (userData.TrialEndDate.HasValue)
            {
                userData.TrialEndDate = userData.TrialEndDate.Value.AddDays(extensionDays);
            }
            else
            {
                userData.TrialEndDate = DateTime.UtcNow.AddDays(extensionDays);
            }
        }

        /// <summary>
        /// Применяет постоянный промокод: проверяет, не использовал ли пользователь, обновляет TrialEndDate и регистрирует использование.
        /// </summary>
        private async Task<CommonResponse> ApplyPermanentPromoCodeAsync(User user, PermanentPromoCode permCode, string userId, IDbContextTransaction transaction)
        {
            bool alreadyUsed = await _context.PermanentPromoCodeUsages
                .AnyAsync(u => u.UserId == userId && u.PermanentPromoCodeId == permCode.Id);
            if (alreadyUsed)
            {
                await transaction.RollbackAsync();
                return new CommonResponse { Success = false, Message = "Promo code has already been used by this user." };
            }

            UpdateTrialEndDate(user.UserData, permCode.ExtensionDays);
            _context.UserData.Update(user.UserData);

            var usage = new PermanentPromoCodeUsage
            {
                UserId = userId,
                PermanentPromoCodeId = permCode.Id,
                UsedOn = DateTime.UtcNow
            };
            _context.PermanentPromoCodeUsages.Add(usage);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new CommonResponse
            {
                Success = true,
                Message = "Permanent promo code applied successfully."
            };
        }

        /// <summary>
        /// Применяет временный промокод: обновляет TrialEndDate, удаляет промокод.
        /// </summary>
        private async Task<CommonResponse> ApplyTemporaryPromoCodeAsync(User user, TemporaryPromoCode tempCode, IDbContextTransaction transaction)
        {
            UpdateTrialEndDate(user.UserData, tempCode.ExtensionDays);
            _context.UserData.Update(user.UserData);

            // Удаляем временный промокод, т.к. он разовый
            _context.TemporaryPromoCodes.Remove(tempCode);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new CommonResponse
            {
                Success = true,
                Message = "Temporary promo code applied successfully."
            };
        }
    }
}
