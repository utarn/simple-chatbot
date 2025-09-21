using ChatbotApi.Application.UserInternalProfiles.Commands.CreateUserInternalProfileCommand;
using ChatbotApi.Application.UserInternalProfiles.Commands.DeleteUserInternalProfileCommand;
using ChatbotApi.Application.UserInternalProfiles.Commands.EditUserInternalProfileCommand;
using ChatbotApi.Application.UserInternalProfiles.Queries.GetUserInternalProfileByIdQuery;
using ChatbotApi.Application.UserInternalProfiles.Queries.GetUserInternalProfilesQuery;
using ChatbotApi.Application.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatbotApi.Web.Controllers;

[Authorize]
public class UserInternalProfilesController : MvcController
{
    public async Task<IActionResult> Index(GetUserInternalProfilesQuery query)
    {
        var result = await Mediator.Send(query);
        return View(result);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserInternalProfileCommand command)
    {
        try
        {
            var result = await Mediator.Send(command);
            return RedirectToAction(nameof(Index), new { createSuccess = true });
        }
        catch (ValidationException e)
        {
            e.AddToModelState(ModelState);
            return View(command);
        }
    }

    public async Task<IActionResult> Details([FromQuery] GetUserInternalProfileByIdQuery query)
    {
        try
        {
            var profile = await Mediator.Send(query);
            return View(profile);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> Edit([FromQuery] GetUserInternalProfileByIdQuery query)
    {
        try
        {
            var profile = await Mediator.Send(query);
            return View(profile);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Edit([FromQuery] GetUserInternalProfileByIdQuery query, EditUserInternalProfileCommand command)
    {
        try
        {
            var result = await Mediator.Send(command);
            return RedirectToAction(nameof(Index), new { editSuccess = true });
        }
        catch (ValidationException e)
        {
            e.AddToModelState(ModelState);
            return View(command);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(DeleteUserInternalProfileCommand command)
    {
        try
        {
            var result = await Mediator.Send(command);
            return Ok();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ValidationException)
        {
            return Forbid();
        }
    }
}