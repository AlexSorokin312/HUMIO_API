using AutoMapper;
using HUMIO_API.DBContext;
using HUMIO_API.Requests;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

public class UserDataService : IUserDataService
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IMapper _mapper;

    public UserDataService(AppDbContext context, UserManager<User> userManager, IMapper mapper)
    {
        _context = context;
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<UserDto> GetUserByIdAsync(string userId)
    {
        var user = await _context.Users
            .Include(u => u.UserData)
            .Include(u => u.UserDevices)
                .ThenInclude(ud => ud.DeviceIdentifier)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new ArgumentException("User not found");

        // Получаем роли пользователя
        var roles = await _userManager.GetRolesAsync(user);

        // Маппим сущность User в UserDto с помощью AutoMapper
        var userDto = _mapper.Map<UserDto>(user);
        userDto.Roles = roles.ToList();

        return userDto;
    }

    public async Task<UserDto> GetUserByEmailAsync(string email)
    {
        var user = await _context.Users
            .Include(u => u.UserData)
            .Include(u => u.UserDevices)
                .ThenInclude(ud => ud.DeviceIdentifier)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            throw new ArgumentException("User not found");

        var roles = await _userManager.GetRolesAsync(user);
        var userDto = _mapper.Map<UserDto>(user);
        userDto.Roles = roles.ToList();

        return userDto;
    }
    
    public async Task<List<DeviceIdentifier>> GetUserDevicesAsync(string userId)
    {
        var devices = await _context.UserDevices
            .Where(ud => ud.UserId == userId)
            .Include(ud => ud.DeviceIdentifier)
            .Select(ud => ud.DeviceIdentifier)
            .ToListAsync();

        return devices;
    }

    public async Task<CommonResponse> RecordPurchaseAndUpdateSubscriptionAsync(string userId, PurchaseRequest request)
    {
        if (string.IsNullOrEmpty(userId))
            Log.Warning("User id is empty");
        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                Log.Information("Starting transaction for user {UserId}", userId);

                var userData = await _context.UserData.FirstOrDefaultAsync(ud => ud.UserId == userId);
                if (userData == null)
                {
                    Log.Warning("User data not found for user {UserId}", userId);
                    return new CommonResponse { Success = false, Message = "User data not found" };
                }

                // Обновляем дату окончания подписки
                userData.SubscriptionEndDate = request.SubscriptionEndDate;
                _context.UserData.Update(userData);

                // Создаем запись покупки
                var purchase = new Purchase
                {
                    UserId = userId,
                    Price = request.Price,
                    PurchaseDate = request.PurchaseDate,
                    SubscriptionEndDate = request.SubscriptionEndDate
                };
                _context.Purchases.Add(purchase);

                // Сохраняем изменения в базе данных
                await _context.SaveChangesAsync();

                // Фиксируем транзакцию
                await transaction.CommitAsync();

                Log.Information("Transaction committed successfully for user {UserId}", userId);

                return new CommonResponse
                {
                    Success = true,
                    Message = "Purchase recorded and subscription updated successfully"
                };
            }
            catch (Exception ex)
            {
                // Откатываем транзакцию в случае ошибки
                await transaction.RollbackAsync();
                Log.Error(ex, "Error in transaction for user {UserId}", userId);
                return new CommonResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
    }

    /// <summary>
    /// Проверяет, существует ли пользователь с указанным email.
    /// Возвращает true, если пользователь найден, иначе false.
    /// </summary>
    public async Task<bool> UserExistsAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            Log.Warning("UserExistsAsync: Передан пустой или пробельный email.");
            return false;
        }

        var user = await _userManager.FindByEmailAsync(email);
        bool exists = user != null;
        Log.Information("UserExistsAsync: Пользователь с email {Email} {Exists}.", email, exists ? "найден" : "не найден");
        return exists;
    }

    /// <summary>
    /// ЭТОТ МЕТОД ТОЛЬКО ДЛЯ ИМПОРТА ПОЛЬЗОВАТЕЛЕЙ!!
    /// </summary>
    /// <param name="email"></param>
    /// <param name="newSubscriptionEndDate"></param>
    /// <returns></returns>
    public async Task<CommonResponse> UpdateSubscriptionEndDateByEmailAsync(string email, DateTime newSubscriptionEndDate)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            Log.Warning("UpdateSubscriptionEndDateByEmailAsync: Передан пустой email.");
            return new CommonResponse { Success = false, Message = "Email обязателен для обновления подписки." };
        }

        try
        {
            // Находим пользователя по email вместе с его данными (UserData)
            var user = await _context.Users
                .Include(u => u.UserData)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                Log.Warning("UpdateSubscriptionEndDateByEmailAsync: Пользователь с email {Email} не найден.", email);
                return new CommonResponse { Success = false, Message = "Пользователь не найден." };
            }

            if (user.UserData == null)
            {
                Log.Warning("UpdateSubscriptionEndDateByEmailAsync: Данные пользователя для email {Email} не найдены.", email);
                return new CommonResponse { Success = false, Message = "Данные пользователя не найдены." };
            }

            // Обновляем дату окончания подписки
            user.UserData.SubscriptionEndDate = newSubscriptionEndDate;
            _context.UserData.Update(user.UserData);

            await _context.SaveChangesAsync();
            Log.Information("UpdateSubscriptionEndDateByEmailAsync: Дата окончания подписки обновлена для пользователя {Email}.", email);

            return new CommonResponse
            {
                Success = true,
                Message = "Дата окончания подписки успешно обновлена."
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "UpdateSubscriptionEndDateByEmailAsync: Ошибка при обновлении подписки для пользователя {Email}.", email);
            return new CommonResponse
            {
                Success = false,
                Message = $"Ошибка: {ex.Message}"
            };
        }
    }

}
