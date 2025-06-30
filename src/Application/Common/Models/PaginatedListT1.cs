using Microsoft.EntityFrameworkCore;

namespace ChatbotApi.Application.Common.Models;

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
