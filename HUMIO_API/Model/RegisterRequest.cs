using System.ComponentModel.DataAnnotations;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [MinLength(6)]
    public string Password { get; set; }

    [Required]
    public string UserName { get; set; }

    public string Country { get; set; }

    // ✅ Теперь можно указывать роль (по умолчанию "User")
    public string Role { get; set; } = "User";
}