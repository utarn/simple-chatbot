using Microsoft.AspNetCore.Identity;
using ChatbotApi.Application.Common.Models;

namespace ChatbotApi.Application.Member.Commands.LoginCommand
{
    public class LoginCommand : IRequest<LoginResult>
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string? ReturnUrl { get; set; }

        public LoginCommand(string username, string password, string? returnUrl)
        {
            Username = username;
            Password = password;
            ReturnUrl = returnUrl;
        }
    }

    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public LoginCommandHandler(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var result = await _signInManager.PasswordSignInAsync(request.Username, request.Password, isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return new LoginResult
                {
                    Success = true,
                    RedirectUrl = !string.IsNullOrEmpty(request.ReturnUrl) ? request.ReturnUrl : "/Member/Index"
                };
            }

            return new LoginResult
            {
                Success = false,
                ErrorMessage = "Invalid username or password."
            };
        }
    }
}
