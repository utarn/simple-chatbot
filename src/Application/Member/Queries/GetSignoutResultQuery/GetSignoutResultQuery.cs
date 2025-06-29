using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ChatbotApi.Application.Member.Queries.GetSignoutResultQuery;

public class GetSignoutResultQuery : IRequest<SignOutResult>
{
    public class GetSignoutResultQueryHandler : IRequestHandler<GetSignoutResultQuery, SignOutResult>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly LinkGenerator _linkGenerator;
        private readonly ISystemService _systemService;
        private readonly UserManager<ApplicationUser> _userManager;

        public GetSignoutResultQueryHandler(ICurrentUserService currentUserService,
            UserManager<ApplicationUser> userManager, LinkGenerator linkGenerator, ISystemService systemService)
        {
            _currentUserService = currentUserService;
            _userManager = userManager;
            _linkGenerator = linkGenerator;
            _systemService = systemService;
        }

        public async Task<SignOutResult> Handle(GetSignoutResultQuery request, CancellationToken cancellationToken)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(_currentUserService.UserId);
            if (user == null)
            {
                return new SignOutResult(new List<string> { "Cookies" });
            }

            IList<UserLoginInfo> logins = await _userManager.GetLoginsAsync(user);
            List<string> authenticationScheme = new List<string> { "Cookies" };
            foreach (UserLoginInfo login in logins)
            {
                // if (login.LoginProvider == "AzureAd")
                // {
                //     authenticationScheme.Add("AzureAd");
                // }

                if (login.LoginProvider == "OpenIdConnect")
                {
                    authenticationScheme.Add("OpenIdConnect");
                }
            }

            string? callbackUrl = _linkGenerator.GetUriByAction(_systemService.HttpContext!, "SignedOut", "Member",
                null, _systemService.HttpContext!.Request.Scheme);
            return new SignOutResult(authenticationScheme.ToArray(),
                new AuthenticationProperties { RedirectUri = callbackUrl });
        }
    }
}
