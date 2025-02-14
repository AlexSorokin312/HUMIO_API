using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class OptionalUserNameValidator<TUser> : UserValidator<TUser> where TUser : class
{
    public OptionalUserNameValidator(IdentityErrorDescriber errors = null)
        : base(errors ?? new IdentityErrorDescriber())
    { }

    public override async Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user)
    {
        var errors = new List<IdentityError>();

        // Получаем UserName
        var userName = await manager.GetUserNameAsync(user);

        // Если UserName не пустой — можно проверить допустимые символы, если требуется.
        if (!string.IsNullOrWhiteSpace(userName))
        {
            var allowedChars = manager.Options.User.AllowedUserNameCharacters;
            if (!userName.All(ch => allowedChars.Contains(ch)))
            {
                errors.Add(new IdentityError
                {
                    Code = "InvalidUserName",
                    Description = "User name contains invalid characters."
                });
            }
        }
        // Не добавляем ошибку, если UserName пустой (то есть делаем его необязательным)

        return errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed(errors.ToArray());
    }
}
