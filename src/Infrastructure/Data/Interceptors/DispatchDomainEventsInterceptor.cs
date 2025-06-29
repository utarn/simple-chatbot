using ChatbotApi.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ChatbotApi.Infrastructure.Data.Interceptors;

public class DispatchDomainEventsInterceptor : SaveChangesInterceptor
{
    private readonly IMediator _mediator;
    private readonly ILogger<DispatchDomainEventsInterceptor> _logger;

    public DispatchDomainEventsInterceptor(IMediator mediator, ILogger<DispatchDomainEventsInterceptor> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        DispatchDomainEvents(eventData.Context, true).GetAwaiter().GetResult();
        var output = base.SavingChanges(eventData, result);
        DispatchDomainEvents(eventData.Context, false).GetAwaiter().GetResult();
        return output;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        await DispatchDomainEvents(eventData.Context, true);
        var output = await base.SavingChangesAsync(eventData, result, cancellationToken);
        await DispatchDomainEvents(eventData.Context, false);
        return output;
    }

    public async Task DispatchDomainEvents(DbContext? context, bool isBefore)
    {
        if (context == null) return;

        var entities = context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity);

        IEnumerable<BaseEntity> baseEntities = entities as BaseEntity[] ?? entities.ToArray();
        var domainEvents = baseEntities
            .SelectMany(e => e.DomainEvents)
            .Where(e => e.IsBeforeSave == isBefore)
            .ToList();

        baseEntities.ToList().ForEach(e => e.ClearDomainEvents(isBefore));

        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent);
        }
    }
}
