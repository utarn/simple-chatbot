using ChatbotApi.Application.Member.Commands.LoginCommand;
using ChatbotApi.Application.Member.Commands.LogoffCommand;
using ChatbotApi.Application.Member.Queries.GetSignoutResultQuery;
using ChatbotApi.Application.View.Queries.GetTopMenuInfoQuery;
using ChatbotApi.Infrastructure.Processors.LLamaPassportProcessor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using ChatbotApi.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ChatbotApi.Web.Controllers
{
    [Authorize]
    public class MemberController : MvcController
    {
        private readonly GoogleSheetHelper _googleSheetHelper;
        private readonly ILogger<GoogleSheetHelper> _logger;
        private readonly IMemoryCache _cache;
        private readonly ISystemService _systemService;

        public MemberController(ILogger<GoogleSheetHelper> logger, IMemoryCache cache, ISystemService systemService)
        {
            _logger = logger;
            _googleSheetHelper = new GoogleSheetHelper(_logger);
            _cache = cache;
            _systemService = systemService;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Index", "Chatbots");
        }

        public async Task<IActionResult> UserInfo(GetTopMenuInfoQuery query)
        {
            var result = await Mediator.Send(query);
            return View(result);
        }

        [AllowAnonymous]
        public IActionResult Login(string? message = null, string? returnUrl = null)
        {
            if (User.Identity is { IsAuthenticated: true })
            {
                if (returnUrl == null)
                {
                    return RedirectToAction(nameof(Index));
                }

                return Redirect(returnUrl);
            }

            ViewData["Message"] = message;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> LoginForm(string username, string password, string? returnUrl = null)
        {
            var command = new LoginCommand(
                username, password, returnUrl
            );
            var result = await Mediator.Send(command);

            if (result.Success)
            {
                return Redirect(result.RedirectUrl ?? Url.Action(nameof(Index))!);
            }

            ViewData["Message"] = result.ErrorMessage ?? "Invalid username or password.";
            return View("Login");
        }

        public async Task<IActionResult> Logoff(GetSignoutResultQuery query)
        {
            SignOutResult result = await Mediator.Send(query);
            return result;
        }

        public async Task<IActionResult> SignedOut()
        {
            if (HttpContext.User.Identity is { IsAuthenticated: true })
            {
                await Mediator.Send(new LogoffCommand());
            }

            return RedirectToAction("Login", new { messsage = "คุณออกจากระบบแล้ว" });
        }

        [HttpGet]
        public IActionResult AllowMail()
        {
            // TODO: Move these to configuration
            var clientId = "YOUR_GOOGLE_CLIENT_ID";
            var redirectUri = _systemService.FullHostName.TrimEnd('/') + "/Member/OAuthCallback"; // Must match Google Console
            var scope = "https://www.googleapis.com/auth/gmail.send";
            var state = Guid.NewGuid().ToString("N"); // Optionally store in session for CSRF protection

            var oauthUrl =
                $"https://accounts.google.com/o/oauth2/v2/auth" +
                $"?client_id={Uri.EscapeDataString(clientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&response_type=code" +
                $"&scope={Uri.EscapeDataString(scope)}" +
                $"&access_type=offline" +
                $"&prompt=consent" +
                $"&state={Uri.EscapeDataString(state)}";

            return Redirect(oauthUrl);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> OAuthCallback(string code, string state)
        {
            // TODO: Move client ID/secret to configuration
            var clientId = "YOUR_GOOGLE_CLIENT_ID";
            var clientSecret = "YOUR_GOOGLE_CLIENT_SECRET";
            var redirectUri = _systemService.FullHostName.TrimEnd('/') + "/Member/OAuthCallback"; // Must match Google Console

            if (string.IsNullOrEmpty(code))
            {
                return BadRequest("Missing authorization code.");
            }

            // Exchange code for tokens using Google's OAuth2 endpoint
            using var httpClient = new HttpClient();
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token");
            tokenRequest.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
            });

            var response = await httpClient.SendAsync(tokenRequest);
            if (!response.IsSuccessStatusCode)
            {
                return BadRequest("Failed to exchange code for tokens.");
            }

            var json = await response.Content.ReadAsStringAsync();
            // You may want to use a DTO for this
            dynamic? tokenResponse = System.Text.Json.JsonSerializer.Deserialize<dynamic>(json);

            // Example: store in IMemoryCache (inject IMemoryCache and ICurrentUserService for user ID)
            // _cache.Set("gmail_token_" + userId, tokenResponse, TimeSpan.FromHours(1));

            // TODO: Implement user identification and secure storage
            // For now, just return the token info for debugging
            return Content(json, "application/json");
        }
    }
}
