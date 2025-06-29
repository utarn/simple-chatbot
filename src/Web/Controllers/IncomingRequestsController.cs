using Microsoft.AspNetCore.Mvc;
using ChatbotApi.Application.IncomingRequests.Queries.GetIncomingRequestsQuery;
using ChatbotApi.Application.Common.Models;
using Microsoft.EntityFrameworkCore;
using ChatbotApi.Application.Common.Interfaces;

namespace ChatbotApi.Web.Controllers
{
    public class IncomingRequestsController : MvcController
    {
        private readonly IApplicationDbContext _context;

        public IncomingRequestsController(IApplicationDbContext context)
        {
            _context = context;
        }

        // GET: IncomingRequests
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 20)
        {
            var requests = await Mediator.Send(new GetIncomingRequestsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            });
            return View(requests);
        }

        // GET: IncomingRequests/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.IncomingRequests
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            return View(request);
        }
    }
}