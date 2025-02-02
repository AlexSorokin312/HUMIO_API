using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HUMIO_API.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthController(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Проверяем, есть ли уже такой пользователь
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
            return BadRequest("User with this email already exists.");

        // Создаем пользователя
        var user = new User
        {
            Email = model.Email,
            UserName = model.UserName,
            Country = model.Country
        };

        // Создаем пользователя в базе данных
        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        // ✅ Добавляем роль (по умолчанию "User", но можно передавать в запросе)
        var role = string.IsNullOrEmpty(model.Role) ? "User" : model.Role;
        await _userManager.AddToRoleAsync(user, role);

        // ✅ Добавляем claims (доп. информация в токене)
        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Role, role),
        new Claim("Country", user.Country ?? ""),
        new Claim("CreatedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
    };
        await _userManager.AddClaimsAsync(user, claims);

        return Ok(new { message = "User registered successfully with role: " + role });
    }



    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return Unauthorized("Invalid email or password");

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);
        if (!result.Succeeded)
            return Unauthorized("Invalid email or password");

        var token = GenerateJwtToken(user);
        return Ok(new { token });
    }


    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { message = "User logged out" });
    }


    private async Task<string> GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // ✅ Получаем все роли пользователя
        var roles = await _userManager.GetRolesAsync(user);

        // ✅ Получаем все claims пользователя
        var userClaims = await _userManager.GetClaimsAsync(user);

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Email, user.Email)
    };

        // ✅ Добавляем роли в claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // ✅ Добавляем кастомные claims
        claims.AddRange(userClaims);

        var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"],
            claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpireMinutes"] ?? "60")),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}
