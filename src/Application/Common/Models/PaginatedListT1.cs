using Microsoft.EntityFrameworkCore;

namespace ChatbotApi.Application.Common.Models;

public class PaginatedList<T1>
{
    public IReadOnlyCollection<T1> Items { get; }
    public int PageIndex { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }

    public int PageSize { get; }

    // Do not use this constructor directly, use the extension methods instead
    public PaginatedList(IReadOnlyCollection<T1> items, int count, int pageNumber, int pageSize)
    {
        PageIndex = pageNumber;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        Items = items;
        PageSize = pageSize;
    }

    public bool HasPreviousPage => PageIndex > 1;

    public bool HasNextPage => PageIndex < TotalPages;

    // Do not use this method directly, use the extension methods instead
    public static async Task<PaginatedList<T1>> CreateAsync(IQueryable<T1> source, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default!)
    {
        var count = await source.CountAsync(cancellationToken);
        var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PaginatedList<T1>(items, count, pageNumber, pageSize);
    }

    // Do not use this method directly, use the extension methods instead
    public static PaginatedList<T1> Create(IList<T1> source, int pageNumber, int pageSize)
    {
        var count = source.Count();
        var items = source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return new PaginatedList<T1>(items, count, pageNumber, pageSize);
    }
}

public static class PaginatedListT1Extensions
{
    // Paging without metadata of filtering, use this with LinQ
    public static Task<PaginatedList<TDestination>> PaginatedListAsync<
        TDestination>(this IQueryable<TDestination> queryable, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default!)
    {
        return PaginatedList<TDestination>.CreateAsync(queryable, pageNumber, pageSize, cancellationToken);
    }


    // No Paging with metadata of filtering, use this with LinQ
    public static Task<List<TDestination>> ProjectToListAsync<TDestination>(this IQueryable queryable,
        IConfigurationProvider configuration)
    {
        return queryable.ProjectTo<TDestination>(configuration).ToListAsync();
    }
}
