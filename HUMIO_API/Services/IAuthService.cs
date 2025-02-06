using Humio.Requests;
using Microsoft.AspNetCore.Identity;

public interface IAuthService
{
    Task<IdentityResult> RegisterAsync(RegisterRequest model);
    Task<TokenResponse> LoginAsync(LoginRequest model);
    Task LogoutAsync();
    Task<UserDto> GetUserAsync(string userId);
    Task<TokenResponse> GoogleAuthAsync(GoogleTokenRequest request);
    Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
}