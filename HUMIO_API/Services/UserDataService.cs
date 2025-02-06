using HUMIO_API.DBContext;
using HUMIO_API.Requests;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class UserDataService : IUserDataService
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;

    public UserDataService(AppDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
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

        var roles = await _userManager.GetRolesAsync(user);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.Name,
            Country = user.UserData?.Country,
            Roles = roles.ToList()
            // Если нужно – можно добавить и список устройств в DTO
        };
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

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.Name,
            Country = user.UserData?.Country,
            Roles = roles.ToList()
        };
    }

    public async Task<List<DeviceIdentifier>> GetUserDevicesAsync(string userId)
    {
        // Извлекаем устройства через связь в таблице UserDevice
        var devices = await _context.UserDevices
            .Where(ud => ud.UserId == userId)
            .Include(ud => ud.DeviceIdentifier)
            .Select(ud => ud.DeviceIdentifier)
            .ToListAsync();

        return devices;
    }

    /// <summary>
    /// Обновляет дату окончания подписки в UserData и сохраняет информацию о покупке.
    /// </summary>
    public async Task RecordPurchaseAndUpdateSubscriptionAsync(PurchaseRequest request)
    {
        // Находим данные пользователя по UserId
        var userData = await _context.UserData.FirstOrDefaultAsync(ud => ud.UserId == request.UserId);
        if (userData == null)
            throw new ArgumentException("User data not found");

        // Обновляем дату окончания подписки
        userData.SubscriptionEndDate = request.SubscriptionEndDate;
        _context.UserData.Update(userData);

        // Создаем запись покупки
        var purchase = new Purchase
        {
            UserId = request.UserId,
            Price = request.Price,
            PurchaseDate = request.PurchaseDate,
            SubscriptionEndDate = request.SubscriptionEndDate
        };
        _context.Purchases.Add(purchase);

        // Сохраняем все изменения
        await _context.SaveChangesAsync();
    }
}
