using System.Security.Claims;
using Humio.Requests;
using HUMIO_API.Model.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Serilog;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService userService)
    {
        _authService = userService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        bool isRegistered = await _authService.RegisterAsync(model);
        if (!isRegistered)
            return BadRequest(new CommonResponse
            {
                Success = false,
                Message = "Registration failed."
            });

        return Ok(new CommonResponse
        {
            Success = true,
            Message = "User registered successfully."
        });
    }


    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest model)
    {
        try
        {
            var token = await _authService.LoginAsync(model);
            return Ok(new { accessToken = token.AccessToken, refreshToken = token.RefreshToken });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return BadRequest(new CommonResponse
            {
                Success = false,
                Message = "Refresh token is required."
            });
        }

        var result = await _authService.LogoutAsync(request.RefreshToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleAuth([FromBody] GoogleTokenRequest request)
    {
        try
        {
            Log.Information("Начало Google аутентификации для AccessToken: {AccessToken}", request.AccessToken);
            var token = await _authService.GoogleAuthAsync(request);
            return Ok(new { accessToken = token.AccessToken, refreshToken = token.RefreshToken });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при Google аутентификации");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("google-no-token")]
    public async Task<IActionResult> GoogleAuthWithoutToken([FromBody] GoogleAuthWithoutTokenRequest request)
    {
        try
        {
            Log.Information("Начало Google аутентификации без токена для пользователя: {Email}, GoogleId: {GoogleId}", request.Email, request.GoogleId);

            // Вызов метода аутентификации без токена
            var token = await _authService.GoogleAuthWithoutTokenAsync(request);

            return Ok(new { accessToken = token.AccessToken, refreshToken = token.RefreshToken });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при Google аутентификации без токена");
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("user")]
    public async Task<IActionResult> GetCurrentUserByToken()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var userDto = await _authService.GetUserAsync(userId);
            return Ok(userDto);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var tokenResponse = await _authService.RefreshTokenAsync(request);
            return Ok(tokenResponse);
        }
        catch (SecurityTokenException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}