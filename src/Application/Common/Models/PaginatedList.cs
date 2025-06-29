namespace ChatbotApi.Application.Common.Models;

/// <summary>
/// ลิสต์แบ่งหน้า
/// </summary>
/// <typeparam name="T1"></typeparam>
/// <typeparam name="T2"></typeparam>
public class PaginatedList<T1, T2> where T2 : class
{
    /// <summary>
    /// รายการ
    /// </summary>
    public IReadOnlyCollection<T1> Items { get; }
    /// <summary>
    /// หน้าที่
    /// </summary>
    public int PageIndex { get; }
    /// <summary>
    /// จำนวนหน้า
    /// </summary>
    public int TotalPages { get; }
    /// <summary>
    /// จำนวนทั้งหมด
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// ข้อมูลเพิ่มเติม
    /// </summary>
    public T2? Metadata { get; }

    /// <summary>
    /// ขนาดหน้า
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// สร้างลิสต์แบ่งหน้า
    /// </summary>
    /// <param name="items"></param>
    /// <param name="count"></param>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <param name="metadata"></param>
    public PaginatedList(IReadOnlyCollection<T1> items, int count, int pageNumber, int pageSize, T2? metadata = null)
    {
        PageIndex = pageNumber;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        Items = items;
        PageSize = pageSize;
        Metadata = metadata;
    }

    /// <summary>
    /// มีหน้าก่อนหน้าหรือไม่
    /// </summary>
    public bool HasPreviousPage => PageIndex > 1;

    /// <summary>
    /// มีหน้าถัดไปหรือไม่
    /// </summary>
    public bool HasNextPage => PageIndex < TotalPages;

    /// <summary>
    /// สร้างลิสต์แบ่งหน้า
    /// </summary>
    /// <param name="source"></param>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <param name="metadata"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<PaginatedList<T1, T2>> CreateAsync(IQueryable<T1> source, int pageNumber, int pageSize, T2? metadata = null, CancellationToken cancellationToken = default!)
    {
        var count = await source.CountAsync(cancellationToken);
        var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PaginatedList<T1, T2>(items, count, pageNumber, pageSize, metadata);
    }

    /// <summary>
    /// สร้างลิสต์แบ่งหน้า
    /// </summary>
    /// <param name="source"></param>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <param name="metadata"></param>
    /// <returns></returns>
    public static PaginatedList<T1, T2> Create(IList<T1> source, int pageNumber, int pageSize, T2? metadata = null)
    {
        var count = source.Count();
        var items = source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return new PaginatedList<T1, T2>(items, count, pageNumber, pageSize, metadata);
    }
}


public class PaginatedList<T1>
{
    /// <summary>
    /// รายการ
    /// </summary>
    public IReadOnlyCollection<T1> Items { get; }
    /// <summary>
    /// หน้าที่
    /// </summary>
    public int PageIndex { get; }
    /// <summary>
    /// จำนวนหน้า
    /// </summary>
    public int TotalPages { get; }
    /// <summary>
    /// จำนวนทั้งหมด
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// ขนาดหน้า
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// สร้างลิสต์แบ่งหน้า
    /// </summary>
    /// <param name="items"></param>
    /// <param name="count"></param>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    public PaginatedList(IReadOnlyCollection<T1> items, int count, int pageNumber, int pageSize)
    {
        PageIndex = pageNumber;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        Items = items;
        PageSize = pageSize;
    }

    /// <summary>
    /// มีหน้าก่อนหน้าหรือไม่
    /// </summary>
    public bool HasPreviousPage => PageIndex > 1;

    /// <summary>
    /// มีหน้าถัดไปหรือไม่
    /// </summary>
    public bool HasNextPage => PageIndex < TotalPages;

    /// <summary>
    /// Do not call CreateAsync directly, use MappingExtensions's PaginatedListAsync instead.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<PaginatedList<T1>> CreateAsync(IQueryable<T1> source, int pageNumber, int pageSize, CancellationToken cancellationToken = default!)
    {
        var count = await source.CountAsync(cancellationToken);
        var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PaginatedList<T1>(items, count, pageNumber, pageSize);
    }

    /// <summary>
    /// สร้างลิสต์แบ่งหน้า
    /// </summary>
    /// <param name="source"></param>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    public static PaginatedList<T1> Create(IList<T1> source, int pageNumber, int pageSize)
    {
        var count = source.Count();
        var items = source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return new PaginatedList<T1>(items, count, pageNumber, pageSize);
    }
}

public static class MappingExtensions
{
    // Paging without metadata of filtering
    public static Task<PaginatedList<TDestination>> PaginatedListAsync<TDestination>(
    this IQueryable<TDestination> queryable, int pageNumber, int pageSize, CancellationToken cancellationToken = default!)
    {
        return PaginatedList<TDestination>.CreateAsync(queryable, pageNumber, pageSize, cancellationToken);
    }


    // Paging with metadata of filtering
    public static Task<PaginatedList<TDestination, TMetaData>> PaginatedListAsync<TDestination, TMetaData>(
        this IQueryable<TDestination> queryable, int pageNumber, int pageSize, TMetaData metadata,
        CancellationToken cancellationToken = default!)
        where TMetaData : class
    {
        return PaginatedList<TDestination, TMetaData>.CreateAsync(queryable, pageNumber, pageSize, metadata, cancellationToken);
    }

    /// <summary>
    /// แปลงเป็น <see cref="List{T}"/>
    /// </summary>
    /// <param name="queryable"></param>
    /// <param name="configuration"></param>
    /// <typeparam name="TDestination"></typeparam>
    /// <returns></returns>
    public static Task<List<TDestination>> ProjectToListAsync<TDestination>(this IQueryable queryable,
        IConfigurationProvider configuration)
    {
        return queryable.ProjectTo<TDestination>(configuration).ToListAsync();
    }


}
