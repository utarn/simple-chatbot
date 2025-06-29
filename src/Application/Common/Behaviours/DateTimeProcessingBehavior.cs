using System.Reflection;

namespace ChatbotApi.Application.Common.Behaviours;

public class DateTimeProcessingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly Dictionary<Type, List<PropertyInfo>> CachedProperties = new();

    private void CacheRequestProperties(Type requestType)
    {
        var propertiesToCache = requestType.GetProperties()
            .Where(p => p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?) ||
                        p.PropertyType == typeof(DateOnly) || p.PropertyType == typeof(DateOnly?))
            .ToList();

        CachedProperties[requestType] = propertiesToCache;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest);
        if (!CachedProperties.ContainsKey(requestType))
        {
            CacheRequestProperties(requestType);
        }

        try
        {
            foreach (var propertyInfo in CachedProperties[requestType])
            {
                object? propertyValue = propertyInfo.GetValue(request);
                switch (propertyValue)
                {
                    case null:
                        continue;
                    case DateTime dateTime:
                        if (dateTime.Kind == DateTimeKind.Unspecified)
                        {
                            dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                        }
                        propertyInfo.SetValue(request, AdjustYear(dateTime));
                        break;
                    case DateOnly dateOnly:
                        DateTime utcDateTime = dateOnly.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                        propertyInfo.SetValue(request, AdjustYear(utcDateTime));
                        break;
                }
            }
        }
        catch (Exception)
        {
            // ignored
        }

        return await next();
    }

    private static DateTime AdjustYear(DateTime dateTime)
    {
        return dateTime.Year switch
        {
            > 2400 => dateTime.AddYears(-543),
            < 1940 => dateTime.AddYears(543),
            _ => dateTime
        };
    }

}

