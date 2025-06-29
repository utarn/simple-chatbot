using ChatbotApi.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatbotApi.Web.Controllers;

[Authorize]
public class GoogleDriveController : MvcController
{
    private readonly IGoogleDriveService _googleDriveService;
    private readonly ICurrentUserService _currentUserService;

    public GoogleDriveController(IGoogleDriveService googleDriveService, ICurrentUserService currentUserService)
    {
        _googleDriveService = googleDriveService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User ID not found");
        var isAuthorized = await _googleDriveService.IsAuthorizedAsync(userId);

        ViewBag.IsAuthorized = isAuthorized;

        if (!isAuthorized)
        {
            TempData["ErrorMessage"] = "You need to authorize Google Drive access first.";
            return View(new List<Google.Apis.Drive.v3.Data.File>());
        }

        var files = await _googleDriveService.ListRootFilesAsync(userId);
        return View(files);
    }

    [HttpGet]
    public IActionResult Authorize()
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User ID not found");
        var authUrl = _googleDriveService.GetAuthorizationUrl(userId);
        return Redirect(authUrl);
    }

    [HttpGet]
    public async Task<IActionResult> Callback(string? code, string? state)
    {
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            TempData["ErrorMessage"] = "Authorization failed. Missing authorization code or state.";
            return RedirectToAction("Index");
        }

        var success = await _googleDriveService.HandleAuthorizationCallbackAsync(state, code);

        if (success)
        {
            TempData["SuccessMessage"] = "Google Drive authorization successful!";
        }
        else
        {
            TempData["ErrorMessage"] = "Authorization failed. Please try again.";
        }

        return RedirectToAction("Index");
    }
}