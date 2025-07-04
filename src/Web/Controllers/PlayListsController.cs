using ChatbotApi.Application.PlayLists.Commands.CreatePlayListCommand;
using ChatbotApi.Application.PlayLists.Commands.EditPlayListCommand;
using ChatbotApi.Application.PlayLists.Commands.DeletePlayListCommand;
using ChatbotApi.Application.PlayLists.Queries.GetPlayListQuery;
using ChatbotApi.Application.PlayLists.Queries.GetPlayListByIdQuery;
using ChatbotApi.Application.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatbotApi.Web.Controllers;

[Authorize]
public class PlayListsController : MvcController
{
    public async Task<IActionResult> Index(GetPlayListQuery query)
    {
        var result = await Mediator.Send(query);
        return View(result);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePlayListCommand command)
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

    public async Task<IActionResult> Edit([FromQuery] GetPlayListByIdQuery query)
    {
        var info = await Mediator.Send(query);
        ViewData["Info"] = info;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Edit([FromQuery] GetPlayListByIdQuery query, EditPlayListCommand command)
    {
        try
        {
            var result = await Mediator.Send(command);
            return RedirectToAction(nameof(Index), new { editSuccess = true });
        }
        catch (ValidationException e)
        {
            e.AddToModelState(ModelState);
            ViewData["Info"] = await Mediator.Send(query);
            return View(command);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(DeletePlayListCommand command)
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

    public async Task<IActionResult> Details([FromQuery] GetPlayListByIdQuery query)
    {
        try
        {
            var result = await Mediator.Send(query);
            return View(result);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }
}