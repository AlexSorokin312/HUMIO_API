using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using Humio.Requests;
using HUMIO_API.DBContext;
using HUMIO_API.Requests;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Serilog;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public AuthService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IConfiguration configuration,
        AppDbContext context,
        IMapper mapper)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _context = context;
        _mapper = mapper;
    }

    #region Public Methods

    public async Task<TokenResponse> LoginAsync(LoginRequest model)
    {
        // Ищем пользователя по email
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            throw new UnauthorizedAccessException("Invalid email or password");

        // Проверяем пароль
        var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);
        if (!result.Succeeded)
            throw new UnauthorizedAccessException("Invalid email or password");

        // При логине – если устройство ещё не привязано к пользователю, привязываем его
        await EnsureUserDeviceBindingAsync(user.Id, model.DeviceIdentifier);

        // Генерируем и возвращаем JWT‑токены
        return await GenerateJwtTokens(user);
    }

    public async Task<bool> RegisterAsync(RegisterRequest model)
    {
        try
        {
            // Проверяем, существует ли уже пользователь с таким email
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                // Если пользователь уже зарегистрирован, но без пароля
                if (string.IsNullOrEmpty(existingUser.PasswordHash))
                {
                    try
                    {
                        // Устанавливаем пароль
                        var addPasswordResult = await _userManager.AddPasswordAsync(existingUser, model.Password);
                        if (!addPasswordResult.Succeeded)
                        {
                            throw new Exception($"Ошибка при добавлении пароля: {string.Join(", ", addPasswordResult.Errors.Select(e => e.Description))}");
                        }

                        // Обновляем UserName и Name
                        existingUser.Name = model.UserName;

                        // Загружаем UserData (если его нет - создаем)
                        var userData = await _context.UserData.FirstOrDefaultAsync(ud => ud.UserId == existingUser.Id);
                        if (userData == null)
                        {
                            userData = new UserData
                            {
                                UserId = existingUser.Id, // Обязательно устанавливаем UserId!
                                Country = model.Country,
                                Platform = model.Platform,
                                PaymentCount = 0,
                                UserName = model.UserName,
                                SubscriptionEndDate = null
                            };
                            _context.UserData.Add(userData);
                        }
                        else
                        {
                            userData.Country = model.Country;
                            userData.Platform = model.Platform;
                            _context.UserData.Update(userData);
                        }

                        await _context.SaveChangesAsync();

                        await BindUserToExistingDeviceAsync(existingUser, model.DeviceIdentifier);

                        // Автоматический вход
                        await _signInManager.SignInAsync(existingUser, isPersistent: false);

                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при обновлении существующего пользователя: {ex.Message}");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("Пользователь с таким email уже существует.");
                    return false;
                }
            }

            // Создаем нового пользователя
            var user = new User
            {
                //UserName = "Default", // Избегаем русских символов
                Email = model.Email,
                Name = model.UserName,
                UserName = model.Email,
                UserData = new UserData
                {
                    UserId = "", // Пока пусто, заполним после создания
                    Country = model.Country,
                    Platform = model.Platform,
                    PaymentCount = 0,
                    UserName = model.UserName,
                    TrialEndDate = DateTime.UtcNow,
                    SubscriptionEndDate = null
                }
            };

            // Создаем пользователя
            var result = await CreateAndSetupUserAsync(user, model.Password, model.Role);
            if (!result.Succeeded)
            {
                throw new Exception($"Ошибка при создании пользователя: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            // Присваиваем ID в UserData и обновляем
            user.UserData.UserId = user.Id;
            _context.UserData.Update(user.UserData);
            await _context.SaveChangesAsync();

            await BindUserToExistingDeviceAsync(user, model.DeviceIdentifier);

            // Автоматический вход
            await _signInManager.SignInAsync(user, isPersistent: false);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка в методе RegisterAsync: {ex.Message}");
            return false;
        }
    }

    public async Task<TokenResponse> GoogleAuthAsync(GoogleTokenRequest request)
    {
        try
        {
            Log.Information("Запрос данных пользователя из Google для AccessToken: {AccessToken}", request.AccessToken);
            var googleUser = await GetGoogleUserInfo(request.AccessToken);

            if (googleUser == null || string.IsNullOrEmpty(googleUser.Email))
            {
                Log.Warning("Не удалось получить данные пользователя от Google.");
                throw new ArgumentException("Не удалось получить данные пользователя от Google.");
            }

            Log.Information("Google пользователь найден: {Email}, {GoogleId}", googleUser.Email, googleUser.Id);
            var user = await _userManager.FindByEmailAsync(googleUser.Email);

            if (user == null)
            {
                Log.Information("Создание нового пользователя {Email}", googleUser.Email);
                user = new User
                {
                    UserName = googleUser.Email,
                    Name = googleUser.Name,
                    Email = googleUser.Email,
                    GoogleId = googleUser.Id,
                    UserData = new UserData
                    {
                        UserName = googleUser.Name,
                        Country = request.Country,
                        Platform = request.Platform,
                        PaymentCount = 0,
                        SubscriptionEndDate = null,
                    }
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    Log.Error("Ошибка при создании пользователя: {Errors}", errors);
                    throw new InvalidOperationException("Ошибка при создании пользователя: " + errors);
                }
                Log.Information("Пользователь {Email} успешно создан", googleUser.Email);
            }
            else
            {
                Log.Information("Пользователь {Email} уже существует", googleUser.Email);
            }

            await BindUserToExistingDeviceAsync(user, request.DeviceIdentifier);
            Log.Information("Пользователь {Email} привязан к устройству {DeviceId}", googleUser.Email, request.DeviceIdentifier);

            var tokens = await GenerateJwtTokens(user);
            Log.Information("JWT токены успешно сгенерированы для {Email}", googleUser.Email);
            return tokens;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при Google аутентификации");
            throw new Exception("Ошибка при Google аутентификации: " + ex.Message, ex);
        }
    }

    public async Task<TokenResponse> GoogleAuthWithoutTokenAsync(GoogleAuthWithoutTokenRequest request)
    {
        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                Log.Information("Запрос данных пользователя из Google. Почта: {Email}, GoogleId: {GoogleId}", request.Email, request.GoogleId);

                // Проверяем, что данные пользователя корректны
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.GoogleId))
                {
                    Log.Warning("Не удалось получить данные пользователя от Google. Почта или GoogleId пустые.");
                    throw new ArgumentException("Не удалось получить данные пользователя от Google.");
                }

                // Ищем пользователя в системе по переданным данным
                var user = await _userManager.FindByEmailAsync(request.Email);

                if (user == null)
                {
                    // Если пользователь не найден, создаем нового
                    Log.Information("Создание нового пользователя {Email}", request.Email);
                    user = new User
                    {
                        UserName = request.Email,
                        Name = request.Name,
                        Email = request.Email,
                        GoogleId = request.GoogleId,
                        UserData = new UserData
                        {
                            UserName = request.Name,
                            Country = request.Country ?? "Unknown",
                            Platform = request.Platform,
                            PaymentCount = 0,
                            SubscriptionEndDate = null,
                        }
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                        Log.Error("Ошибка при создании пользователя: {Errors}", errors);
                        throw new InvalidOperationException("Ошибка при создании пользователя: " + errors);
                    }
                    Log.Information("Пользователь {Email} успешно создан", request.Email);
                }
                else
                {
                    // Если пользователь уже существует
                    Log.Information("Пользователь {Email} уже существует", request.Email);
                }

                // Привязываем пользователя к устройству
                await BindUserToExistingDeviceAsync(user, request.DeviceIdentifier);
                Log.Information("Пользователь {Email} привязан к устройству {DeviceId}", request.Email, request.DeviceIdentifier);

                // Генерируем JWT токены для пользователя
                var tokens = await GenerateJwtTokens(user);
                Log.Information("JWT токены успешно сгенерированы для {Email}", request.Email);

                // Коммитим транзакцию, так как все операции прошли успешно
                await transaction.CommitAsync();

                return tokens;
            }
            catch (Exception ex)
            {
                // Если возникает ошибка, откатываем транзакцию
                await transaction.RollbackAsync();
                Log.Error(ex, "Ошибка при Google аутентификации");
                throw new Exception("Ошибка при Google аутентификации: " + ex.Message, ex);
            }
        }
    }


    public async Task<UserDto> GetUserAsync(string userId)
    {
        // Жадно загружаем связанные сущности
        var user = await _context.Users
            .Include(u => u.UserData)
            .Include(u => u.UserDevices)
                .ThenInclude(ud => ud.DeviceIdentifier)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new ArgumentException("User not found.");

        // Маппим объект User в UserDto с помощью AutoMapper
        var userDto = _mapper.Map<UserDto>(user);

        // Получаем роли отдельно, так как они не включены в маппинг профиля
        var roles = await _userManager.GetRolesAsync(user);
        userDto.Roles = roles.ToList();

        return userDto;
    }

    public async Task<CommonResponse> LogoutAsync(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return new CommonResponse
            {
                Success = false,
                Message = "Refresh token is required."
            };
        }

        var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
        if (storedToken == null)
        {
            return new CommonResponse
            {
                Success = false,
                Message = "Refresh token not found."
            };
        }

        storedToken.IsRevoked = true;
        await _context.SaveChangesAsync();

        return new CommonResponse
        {
            Success = true,
            Message = "User logged out successfully."
        };
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Создаёт пользователя, назначает ему роль и добавляет клаймы.
    /// </summary>
    private async Task<IdentityResult> CreateAndSetupUserAsync(User user, string password, string role)
    {
        try
        {
            user.UserName = user.Email;
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                return result;

            role = string.IsNullOrEmpty(role) ? "User" : role;
            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
                return roleResult;

            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, role),
            new Claim("Country", user.UserData?.Country ?? ""),
            new Claim("CreatedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
        };

            await _userManager.AddClaimsAsync(user, claims);
            return result;
        }
        catch (Exception ex)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "Exception",
                Description = ex.Message
            });
        }
    }

    /// <summary>
    /// Производит привязку пользователя к устройству:
    /// 1. Находит запись DeviceIdentifier по значению deviceIdentifier.
    /// 2. Если запись найдена, создаёт связь (UserDevice).
    /// 3. Если устройство содержит TrialEndDate, копирует его в UserData.SubscriptionEndDate.
    /// </summary>
    private async Task BindUserToExistingDeviceAsync(User user, string deviceIdentifier)
    {
        if (user == null)
            return;

        // Находим устройство по заданному DeviceIdentifier
        var device = await _context.DeviceIdentifiers
            .Include(d => d.UserDevices)
            .FirstOrDefaultAsync(d => d.DeviceId == deviceIdentifier);

        if (device == null)
            throw new InvalidOperationException("Устройство не найдено. Оно должно быть создано до регистрации пользователя.");

        // Если связь уже установлена, ничего не делаем
        if (!device.UserDevices.Any(ud => ud.UserId == user.Id))
        {
            _context.UserDevices.Add(new UserDevice
            {
                UserId = user.Id,
                DeviceId = device.Id
            });
            await _context.SaveChangesAsync();
        }

        // Гарантируем, что у пользователя загружен объект UserData
        if (user.UserData == null)
        {
            // Пытаемся загрузить UserData из базы
            user.UserData = await _context.UserData.FirstOrDefaultAsync(ud => ud.UserId == user.Id);
            // Если и в базе записи нет, инициализируем новый объект (при необходимости можно добавить сохранение в базу)
            if (user.UserData == null)
            {
                user.UserData = new UserData { UserId = user.Id };
                _context.UserData.Add(user.UserData);
                await _context.SaveChangesAsync();
            }
        }

        // Если у устройства задан TrialEndDate, копируем его в UserData.TrialEndDate
        if (device.TrialEndDate.HasValue)
        {
            user.UserData.TrialEndDate = device.TrialEndDate;
            await _userManager.UpdateAsync(user);
        }
    }

    /// <summary>
    /// Если пользователь уже существует, при входе проверяет, привязан ли к нему указанный deviceIdentifier.
    /// Если нет – создаёт связь.
    /// </summary>
    private async Task EnsureUserDeviceBindingAsync(string userId, string deviceIdentifier)
    {
        var device = await _context.DeviceIdentifiers
            .Include(d => d.UserDevices)
            .FirstOrDefaultAsync(d => d.DeviceId == deviceIdentifier);

        if (device == null)
            throw new InvalidOperationException("Устройство не найдено.");

        if (!device.UserDevices.Any(ud => ud.UserId == userId))
        {
            _context.UserDevices.Add(new UserDevice
            {
                UserId = userId,
                DeviceId = device.Id
            });
            await _context.SaveChangesAsync();
        }
    }

    private async Task<TokenResponse> GenerateJwtTokens(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var roles = await _userManager.GetRolesAsync(user);
        var userClaims = await _userManager.GetClaimsAsync(user);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(userClaims);

        var expiresAt = DateTime.UtcNow.AddHours(1);
        var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"],
            claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateRefreshToken();
        await SaveRefreshTokenAsync(user, refreshToken);

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var principal = GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
            throw new SecurityTokenException("Invalid access token.");

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            throw new SecurityTokenException("Invalid access token.");

        var storedRefreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken && x.UserId == userId);

        if (storedRefreshToken == null || storedRefreshToken.Expires < DateTime.UtcNow || storedRefreshToken.IsRevoked)
            throw new SecurityTokenException("Invalid refresh token.");

        storedRefreshToken.IsRevoked = true;
        await _context.SaveChangesAsync();

        await CleanupExpiredRefreshTokensAsync(userId);

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            throw new SecurityTokenException("User not found.");

        return await GenerateJwtTokens(user);
    }

    private async Task SaveRefreshTokenAsync(User user, string refreshToken)
    {
        var tokenEntry = new RefreshToken
        {
            Token = refreshToken,
            Expires = DateTime.UtcNow.AddDays(30),
            IsRevoked = false,
            UserId = user.Id
        };

        _context.RefreshTokens.Add(tokenEntry);
        await _context.SaveChangesAsync();
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }

    private async Task CleanupExpiredRefreshTokensAsync(string userId)
    {
        var expiredTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.Expires < DateTime.UtcNow)
            .ToListAsync();

        if (expiredTokens.Any())
        {
            _context.RefreshTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();
        }
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        tokenHandler.InboundClaimTypeMap.Clear();
        SecurityToken securityToken;
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);

        if (!(securityToken is JwtSecurityToken jwtSecurityToken) ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;
    }

    private async Task<GoogleUserResponse> GetGoogleUserInfo(string accessToken)
    {
        var googleUserInfoUrl = "https://www.googleapis.com/oauth2/v2/userinfo";
        using (var httpClient = new HttpClient())
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, googleUserInfoUrl);
            requestMessage.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
                throw new UnauthorizedAccessException("Invalid access token.");

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<GoogleUserResponse>(jsonResponse);
        }
    }

    #endregion
}
