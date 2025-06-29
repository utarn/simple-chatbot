using ChatbotApi.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ChatbotApi.Infrastructure.Identity;

public class SimpleUserManager : UserManager<ApplicationUser>
{
    public SimpleUserManager(IUserStore<ApplicationUser> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<ApplicationUser> passwordHasher, IEnumerable<IUserValidator<ApplicationUser>> userValidators, IEnumerable<IPasswordValidator<ApplicationUser>> passwordValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<ApplicationUser>> logger)
        : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
    {
    }

    public override async Task<IdentityResult> CreateAsync(ApplicationUser user)
    {
        if (user.Email == null)
        {
            throw new ArgumentNullException(nameof(user.Email));
        }
        user.Id = user.Email.Replace("@","_").ToLower();
        return await base.CreateAsync(user);
    }
}
