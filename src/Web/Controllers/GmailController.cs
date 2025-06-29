using ChatbotApi.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatbotApi.Web.Controllers;

public class GmailController : MvcController
{
    private readonly IGmailService _gmailService;
    private readonly ICurrentUserService _currentUserService;

    public GmailController(IGmailService gmailService, ICurrentUserService currentUserService)
    {
        _gmailService = gmailService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Initiates Gmail OAuth authorization flow
    /// </summary>
    /// <returns>Redirect to Google OAuth</returns>
    [HttpGet]
    [Authorize]
    public IActionResult Authorize()
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User ID not found");
        var authUrl = _gmailService.GetAuthorizationUrl(userId);
        return Redirect(authUrl);
    }

    /// <summary>
    /// Handles OAuth callback from Google
    /// </summary>
    /// <param name="code">Authorization code from Google</param>
    /// <param name="state">User ID passed in authorization request</param>
    /// <returns>Redirect to success or error page</returns>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Callback(string? code, string? state)
    {
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            TempData["ErrorMessage"] = "Authorization failed. Missing authorization code or state.";
            return RedirectToAction("AuthorizationResult");
        }

        var success = await _gmailService.HandleAuthorizationCallbackAsync(state, code);

        if (success)
        {
            TempData["SuccessMessage"] = "Gmail authorization successful! You can now send emails.";
        }
        else
        {
            TempData["ErrorMessage"] = "Authorization failed. Please try again.";
        }

        return RedirectToAction("Index");
    }

    /// <summary>
    /// Displays the main Gmail page, checking for authorization.
    /// </summary>
    /// <returns>View with authorization status or redirects to Compose.</returns>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User ID not found");
        var isAuthorized = await _gmailService.IsAuthorizedAsync(userId);

        ViewBag.IsAuthorized = isAuthorized;

        if (!isAuthorized)
        {
            TempData["ErrorMessage"] = "You need to authorize Gmail access first.";
        }

        return View();
    }

    /// <summary>
    /// Shows compose email page
    /// </summary>
    /// <returns>Compose email view</returns>
    [HttpGet]
    [Authorize]
    public IActionResult Compose()
    {
        return View();
    }

    /// <summary>
    /// Sends email via Gmail API
    /// </summary>
    /// <param name="model">Email compose model</param>
    /// <returns>Result of email sending</returns>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Compose(ComposeEmailModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User ID not found");
        var isAuthorized = await _gmailService.IsAuthorizedAsync(userId);

        if (!isAuthorized)
        {
            TempData["ErrorMessage"] = "You need to authorize Gmail access first.";
            return RedirectToAction("Authorize");
        }

        var success = await _gmailService.SendEmailAsync(userId, model.To, model.Subject, model.Body);

        if (success)
        {
            TempData["SuccessMessage"] = $"Email sent successfully to {model.To}";
            return RedirectToAction("Compose");
        }
        else
        {
            ModelState.AddModelError("", "Failed to send email. Please try again.");
            return View(model);
        }
    }

    /// <summary>
    /// API endpoint to send email
    /// </summary>
    /// <param name="model">Email compose model</param>
    /// <returns>JSON result</returns>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SendEmail([FromBody] ComposeEmailModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User ID not found");
        var isAuthorized = await _gmailService.IsAuthorizedAsync(userId);

        if (!isAuthorized)
        {
            return Unauthorized(new { message = "Gmail authorization required" });
        }

        var success = await _gmailService.SendEmailAsync(userId, model.To, model.Subject, model.Body);

        if (success)
        {
            return Ok(new { message = "Email sent successfully", to = model.To });
        }
        else
        {
            return BadRequest(new { message = "Failed to send email" });
        }
    }

    /// <summary>
    /// Checks if user has Gmail authorization
    /// </summary>
    /// <returns>JSON result with authorization status</returns>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> IsAuthorized()
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User ID not found");
        var isAuthorized = await _gmailService.IsAuthorizedAsync(userId);

        return Ok(new { isAuthorized });
    }

    /// <summary>
    /// Revokes Gmail authorization
    /// </summary>
    /// <returns>JSON result</returns>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> RevokeAuthorization()
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User ID not found");
        var success = await _gmailService.RevokeAuthorizationAsync(userId);

        if (success)
        {
            return Ok(new { message = "Authorization revoked successfully" });
        }
        else
        {
            return BadRequest(new { message = "Failed to revoke authorization" });
        }
    }

    /// <summary>
    /// Gets latest emails from last 4 hours with duplicate tracking
    /// </summary>
    /// <returns>JSON result with email list</returns>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetLatestEmails()
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User ID not found");
        var isAuthorized = await _gmailService.IsAuthorizedAsync(userId);

        if (!isAuthorized)
        {
            return Unauthorized(new { message = "Gmail authorization required" });
        }

        try
        {
            var emails = await _gmailService.GetLatestEmailsAsync(userId);
            return Ok(new
            {
                emails,
                count = emails.Count,
                message = $"Retrieved {emails.Count} new emails"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Failed to retrieve emails: {ex.Message}" });
        }
    }
}

/// <summary>
/// Model for composing emails
/// </summary>
public class ComposeEmailModel
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}