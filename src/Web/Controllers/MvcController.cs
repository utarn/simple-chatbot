using System.Globalization;
using AutoMapper;
using ChatbotApi.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ChatbotApi.Web.Controllers;

public abstract class MvcController : Controller
{
    private IMapper? _mapper;
    private ISender? _mediator;
    private IAuthorizationService? _authorizationService;
    private IWebHostEnvironment? _webHostEnvironment;
    protected IWebHostEnvironment WebHostEnvironment =>
        _webHostEnvironment ??= HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
    protected IMapper Mapper => _mapper ??= HttpContext.RequestServices.GetRequiredService<IMapper>();
    protected IAuthorizationService AuthorizationService =>
        _authorizationService ??= HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();

    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        base.OnActionExecuting(filterContext);
    }
}
