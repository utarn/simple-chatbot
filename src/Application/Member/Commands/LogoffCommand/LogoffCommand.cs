using ChatbotApi.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ChatbotApi.Application.Member.Commands.LogoffCommand;

public class LogoffCommand : IRequest<Unit>
{
    public class LogoffCommandHandler : IRequestHandler<LogoffCommand, Unit>
    {
        private readonly ILogger<LogoffCommandHandler> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public LogoffCommandHandler(SignInManager<ApplicationUser> signInManager, ILogger<LogoffCommandHandler> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        public async Task<Unit> Handle(LogoffCommand request, CancellationToken cancellationToken)
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation(4, "User logged out.");
            return Unit.Value;
        }
    }
}
