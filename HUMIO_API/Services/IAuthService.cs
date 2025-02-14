using Humio.Requests;

public interface IAuthService
{
    Task<bool> RegisterAsync(RegisterRequest model);
    Task<TokenResponse> LoginAsync(LoginRequest model);
    Task<CommonResponse> LogoutAsync(string refreshToken);
    Task<UserDto> GetUserAsync(string userId);
    Task<TokenResponse> GoogleAuthAsync(GoogleTokenRequest request);
    Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
}