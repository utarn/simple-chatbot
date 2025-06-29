using OpenAiService.Interfaces;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace ChatbotApi.Application.Chatbots.Queries.GetModelHarborModelsQuery;

public class GetModelHarborModelsQuery : IRequest<SelectList>
{
    public object? SelectedValue { get; set; }
}

public class GetModelHarborModelsQueryHandler : IRequestHandler<GetModelHarborModelsQuery, SelectList>
{
    private readonly IOpenAiService _openAiService;
    private readonly ILogger<GetModelHarborModelsQueryHandler> _logger;
    private readonly IMemoryCache _memoryCache;

    private const string CacheKey = "GetModelHarborModelsQuery_SelectList";

    public GetModelHarborModelsQueryHandler(
        IOpenAiService openAiService,
        ILogger<GetModelHarborModelsQueryHandler> logger,
        IMemoryCache memoryCache)
    {
        _openAiService = openAiService;
        _logger = logger;
        _memoryCache = memoryCache;
    }

    public async Task<SelectList> Handle(GetModelHarborModelsQuery request, CancellationToken cancellationToken)
    {
        if (_memoryCache.TryGetValue(CacheKey, out SelectList? cachedSelectList) && cachedSelectList != null)
        {
            return cachedSelectList;
        }

        try
        {
            var models = await _openAiService.GetModelsAsync(cancellationToken);

            var selectListItems = models
                .Select(model => new SelectListItem
                {
                    Value = model.Value,
                    Text = model.Text
                })
                .ToList();

            var selectList = new SelectList(selectListItems, "Value", "Text", request.SelectedValue);

            _memoryCache.Set(CacheKey, selectList, TimeSpan.FromSeconds(300));

            return selectList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching models from ModelHarbor API");
            throw new ApplicationException("Failed to retrieve models from ModelHarbor API.", ex);
        }
    }
}
