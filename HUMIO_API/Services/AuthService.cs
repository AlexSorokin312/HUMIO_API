using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Humio.Requests;
using HUMIO_API.DBContext;
using HUMIO_API.Requests;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;

    public AuthService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IConfiguration configuration,
        AppDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _context = context;
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

    public async Task<IdentityResult> RegisterAsync(RegisterRequest model)
    {
        // Проверяем, существует ли уже пользователь с таким email
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            // Если пользователь существует, проверяем, установлен ли у него пароль
            if (string.IsNullOrEmpty(existingUser.PasswordHash))
            {
                // Пользователь существует, но не имеет пароля — добавляем его
                var addPasswordResult = await _userManager.AddPasswordAsync(existingUser, model.Password);
                if (!addPasswordResult.Succeeded)
                {
                    return IdentityResult.Failed(new IdentityError { Description = "Unable to set the password." });
                }

                // Дополнительно можно обновить данные, если они отличаются от новых данных регистрации.
                existingUser.UserName = model.UserName.Replace(" ", string.Empty);
                existingUser.Name = model.UserName;
                // Можно обновить и связанные данные пользователя (UserData), если требуется:
                if (existingUser.UserData == null)
                {
                    existingUser.UserData = new UserData
                    {
                        Country = model.Country,
                        Platform = model.Platform,
                        PaymentCount = 0,
                        SubscriptionEndDate = null
                    };
                }
                else
                {
                    existingUser.UserData.Country = model.Country;
                    existingUser.UserData.Platform = model.Platform;
                }

                var updateResult = await _userManager.UpdateAsync(existingUser);
                if (!updateResult.Succeeded)
                {
                    return IdentityResult.Failed(new IdentityError { Description = "Failed to update user data." });
                }

                // Привязка устройства (если нужно обновить привязку)
                await BindUserToExistingDeviceAsync(existingUser, model.DeviceIdentifier);

                return IdentityResult.Success;
            }
            else
            {
                // Если пароль уже установлен — сообщаем, что пользователь существует.
                return IdentityResult.Failed(new IdentityError { Description = "User with that email already exists." });
            }
        }

        // Если пользователя нет — создаём нового со всеми данными и паролем.
        var user = new User
        {
            UserName = model.UserName.Replace(" ", string.Empty),
            Email = model.Email,
            Name = model.UserName,
            UserData = new UserData
            {
                Country = model.Country,
                Platform = model.Platform,
                PaymentCount = 0,
                SubscriptionEndDate = null // будет обновлено, если устройство имеет TrialEndDate
            }
        };

        var result = await CreateAndSetupUserAsync(user, model.Password, model.Role);
        if (!result.Succeeded)
            return result;

        await BindUserToExistingDeviceAsync(user, model.DeviceIdentifier);

        return result;
    }

    public async Task<TokenResponse> GoogleAuthAsync(GoogleTokenRequest request)
    {
        var googleUser = await GetGoogleUserInfo(request.AccessToken);
        if (googleUser == null || string.IsNullOrEmpty(googleUser.Email))
            throw new ArgumentException("Не удалось получить данные пользователя.");

        var user = await _userManager.FindByEmailAsync(googleUser.Email);
        if (user == null)
        {
            user = new User
            {
                UserName = googleUser.Name.Replace(" ", string.Empty),
                Name = googleUser.Name,
                Email = googleUser.Email,
                GoogleId = googleUser.Id,
                UserData = new UserData
                {
                    Country = request.Country,
                    Platform = request.Platform,
                    PaymentCount = 0,
                    SubscriptionEndDate = null
                }
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
                throw new InvalidOperationException(string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }

        await EnsureUserDeviceBindingAsync(user.Id, request.DeviceIdentifier);

        return await GenerateJwtTokens(user);
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

    public async Task<UserDto> GetUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            throw new ArgumentException("User not found.");

        return await MapUserToDto(user);
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Создаёт пользователя, назначает ему роль и добавляет клаймы.
    /// </summary>
    private async Task<IdentityResult> CreateAndSetupUserAsync(User user, string password, string role)
    {
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

    /// <summary>
    /// Производит привязку пользователя к устройству:
    /// 1. Находит запись DeviceIdentifier по значению deviceIdentifier.
    /// 2. Если запись найдена, создаёт связь (UserDevice).
    /// 3. Если устройство содержит TrialEndDate, копирует его в UserData.SubscriptionEndDate.
    /// </summary>
    private async Task BindUserToExistingDeviceAsync(User user, string deviceIdentifier)
    {
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

        // Копируем trial дату из устройства в UserData, если она установлена
        if (device.TrialEndDate.HasValue)
        {
            user.UserData.SubscriptionEndDate = device.TrialEndDate;
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

    private async Task SaveRefreshTokenAsync(User user, string refreshToken)
    {
        var tokenEntry = new RefreshToken
        {
            Token = refreshToken,
            Expires = DateTime.UtcNow.AddDays(7),
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

    private async Task<UserDto> MapUserToDto(User user)
    {
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

    private async Task<GoogleUserResponce> GetGoogleUserInfo(string accessToken)
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
            return JsonConvert.DeserializeObject<GoogleUserResponce>(jsonResponse);
        }
    }

    #endregion
}
