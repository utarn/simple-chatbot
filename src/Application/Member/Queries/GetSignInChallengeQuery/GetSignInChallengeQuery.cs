using ChatbotApi.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ChatbotApi.Application.Member.Queries.GetSignInChallengeQuery;

public class GetSignInChallengeQuery : IRequest<ChallengeResult>
{
    public string Provider { get; set; } = default!;
    public string ReturnUrl { get; set; } = default!;

    public class GetSignInChallengeQueryHandler : IRequestHandler<GetSignInChallengeQuery, ChallengeResult>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly LinkGenerator _linkGenerator;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public GetSignInChallengeQueryHandler(
            IHttpContextAccessor httpContextAccessor,
            LinkGenerator linkGenerator,
            SignInManager<ApplicationUser> signInManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _linkGenerator = linkGenerator;
            _signInManager = signInManager;
        }

        public Task<ChallengeResult> Handle(GetSignInChallengeQuery request, CancellationToken cancellationToken)
        {
            string? redirectUrl = _linkGenerator.GetUriByAction(_httpContextAccessor.HttpContext!,
                "ExternalLoginCallback", "Member", new { request.ReturnUrl });
            AuthenticationProperties? properties =
                _signInManager.ConfigureExternalAuthenticationProperties(request.Provider, redirectUrl);
            ChallengeResult result = new(request.Provider, properties);
            return Task.FromResult(result);
        }
    }
}
