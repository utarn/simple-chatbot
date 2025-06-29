using ChatbotApi.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatbotApi.Web.Controllers;

[Authorize]
public class CalendarController : MvcController
{
    private readonly ICalendarService _calendarService;
    private readonly ICurrentUserService _currentUserService;

    public CalendarController(ICalendarService calendarService, ICurrentUserService currentUserService)
    {
        _calendarService = calendarService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Initiates Calendar OAuth authorization flow
    /// </summary>
    /// <returns>Redirect to Google OAuth</returns>
    [HttpGet]
    public IActionResult Authorize()
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User ID not found");
        var authUrl = _calendarService.GetAuthorizationUrl(userId);
        return Redirect(authUrl);
    }

    /// <summary>
    /// Handles OAuth callback from Google
    /// </summary>
    /// <param name="code">Authorization code from Google</param>
    /// <param name="state">User ID passed in authorization request</param>
    /// <returns>Redirect to success or error page</returns>
    [HttpGet]
    public async Task<IActionResult> Callback(string? code, string? state)
    {
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            TempData["ErrorMessage"] = "Authorization failed. Missing authorization code or state.";
            return RedirectToAction("AuthorizationResult");
        }

        var success = await _calendarService.HandleAuthorizationCallbackAsync(state, code);

        if (success)
        {
            TempData["SuccessMessage"] = "Calendar authorization successful! You can now create calendar events.";
        }
        else
        {
            TempData["ErrorMessage"] = "Authorization failed. Please try again.";
        }

        return RedirectToAction("Index");
    }

    /// <summary>
    /// Shows calendar management page
    /// </summary>
    /// <returns>Calendar view</returns>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User ID not found");
        var isAuthorized = await _calendarService.IsAuthorizedAsync(userId);

        ViewBag.IsAuthorized = isAuthorized;

        if (!isAuthorized)
        {
            TempData["ErrorMessage"] = "You need to authorize Calendar access first.";
        }

        return View();
    }

    /// <summary>
    /// API endpoint to create a calendar event
    /// </summary>
    /// <param name="model">Calendar event model</param>
    /// <returns>JSON result</returns>
    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User ID not found");
        var isAuthorized = await _calendarService.IsAuthorizedAsync(userId);

        if (!isAuthorized)
        {
            return Unauthorized(new { message = "Calendar authorization required" });
        }

        var success = await _calendarService.CreateEventAsync(
            userId,
            model.Summary,
            model.Description ?? string.Empty,
            model.StartDateTime,
            model.EndDateTime,
            model.IsAllDay);

        if (success)
        {
            return Ok(new { message = "Event created successfully", summary = model.Summary });
        }
        else
        {
            return BadRequest(new { message = "Failed to create event" });
        }
    }

    /// <summary>
    /// Checks if user has Calendar authorization
    /// </summary>
    /// <returns>JSON result with authorization status</returns>
    [HttpGet]
    public async Task<IActionResult> IsAuthorized()
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User ID not found");
        var isAuthorized = await _calendarService.IsAuthorizedAsync(userId);

        return Ok(new { isAuthorized });
    }

    /// <summary>
    /// Revokes Calendar authorization
    /// </summary>
    /// <returns>JSON result</returns>
    [HttpPost]
    public async Task<IActionResult> RevokeAuthorization()
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User ID not found");
        var success = await _calendarService.RevokeAuthorizationAsync(userId);

        if (success)
        {
            return Ok(new { message = "Authorization revoked successfully" });
        }
        else
        {
            return BadRequest(new { message = "Failed to revoke authorization" });
        }
    }
}

/// <summary>
/// Model for creating calendar events
/// </summary>
public class CreateEventModel
{
    public string Summary { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public bool IsAllDay { get; set; }
}