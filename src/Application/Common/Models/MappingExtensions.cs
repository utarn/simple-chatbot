using AutoMapper.QueryableExtensions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace ChatbotApi.Application.Common.Models;

public static class MappingExtensions
{
    // Paging without metadata of filtering
    public static Task<PaginatedList<TDestination>> PaginatedListAsync<
        TDestination>(this IQueryable<TDestination> queryable, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default!)
    {
        return PaginatedList<TDestination>.CreateAsync(queryable, pageNumber, pageSize, cancellationToken);
    }


    // Paging with metadata of filtering
    public static Task<PaginatedList<TDestination, TMetaData>> PaginatedListAsync<
        TDestination, TMetaData>(
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
