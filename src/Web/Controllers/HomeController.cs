using ChatbotApi.Application.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ChatbotApi.Web.Controllers;

public class HomeController : MvcController
{

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(string? message = null, string? returnUrl = null)
    {
        IExceptionHandlerPathFeature? exceptionHandlerPathFeature =
            HttpContext.Features.Get<IExceptionHandlerPathFeature>();

        if (message == null)
        {
            if (exceptionHandlerPathFeature?.Error is ValidationException exception)
            {
                ViewData["Message"] = string.Join("<br>", exception.Errors.SelectMany(e => e.Value));
            }
            else
            {
                ViewData["Message"] = "เกิดข้อผิดพลาด";
            }
        }
        else
        {
            ViewData["Message"] = message;
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View("Error");
    }
}
