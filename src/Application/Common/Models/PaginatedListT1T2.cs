using Microsoft.EntityFrameworkCore;

namespace ChatbotApi.Application.Common.Models;

public class PaginatedList<T1, T2> where T2 : class
{
    public IReadOnlyCollection<T1> Items { get; }
    public int PageIndex { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }
    public T2? Metadata { get; }
    public int PageSize { get; }
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;
    // Do not use this constructor directly, use the extension methods instead
    public PaginatedList(IReadOnlyCollection<T1> items, int count, int pageNumber, int pageSize, T2? metadata = null)
    {
        PageIndex = pageNumber;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        Items = items;
        PageSize = pageSize;
        Metadata = metadata;
    }

    // Do not use this method directly, use the extension methods instead
    public static async Task<PaginatedList<T1, T2>> CreateAsync(IQueryable<T1> source, int pageNumber, int pageSize, T2? metadata = null, CancellationToken cancellationToken = default!)
    {
        var count = await source.CountAsync(cancellationToken);
        var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PaginatedList<T1, T2>(items, count, pageNumber, pageSize, metadata);
    }
    // Do not use this method directly, use the extension methods instead
    public static PaginatedList<T1, T2> Create(IList<T1> source, int pageNumber, int pageSize, T2? metadata = null)
    {
        var count = source.Count();
        var items = source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return new PaginatedList<T1, T2>(items, count, pageNumber, pageSize, metadata);
    }
}

public static class PaginatedListT1T2Extensions
{
    // Paging with metadata of filtering, use this with LinQ
    public static Task<PaginatedList<TDestination, TMetaData>> PaginatedListAsync<
        TDestination, TMetaData>(
        this IQueryable<TDestination> queryable, int pageNumber, int pageSize, TMetaData metadata,
        CancellationToken cancellationToken = default!)
        where TMetaData : class
    {
        return PaginatedList<TDestination, TMetaData>.CreateAsync(queryable, pageNumber, pageSize, metadata, cancellationToken);
    }
}
