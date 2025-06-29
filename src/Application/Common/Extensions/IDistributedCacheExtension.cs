using Hyperion;
using Microsoft.Extensions.Caching.Distributed;

namespace ChatbotApi.Application.Common.Extensions;

public static class CacheExtension
{
    public static async Task SetObjectAsync<T>(this IDistributedCache cache, string id, T value,
        double lifespan = 14400, bool sliding = true, bool preserve = true)
    {
        Serializer serializer = new Serializer();
        serializer.Options.WithPreserveObjectReferences(preserve);
        await using MemoryStream mem = new MemoryStream();
        serializer.Serialize(value, mem);
        byte[] toWrite = mem.ToArray();
        if (sliding)
        {
            await cache.SetAsync(id, toWrite,
                new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(lifespan) });
        }
        else
        {
            await cache.SetAsync(id, toWrite,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(lifespan) });
        }
    }

    public static async Task SetObjectAsync<T>(this IDistributedCache cache, string id, T value,
        DateTimeOffset endofLife)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(id);
        Serializer serializer = new Serializer();
        serializer.Options.WithPreserveObjectReferences(true);
        await using MemoryStream mem = new MemoryStream();
        serializer.Serialize(value, mem);
        byte[] toWrite = mem.ToArray();
        await cache.SetAsync(id, toWrite, new DistributedCacheEntryOptions { AbsoluteExpiration = endofLife });
    }

    public static async Task<T?> GetObjectAsync<T>(this IDistributedCache cache, string? id)
    {
        if (id == null)
        {
            return default;
        }
        try
        {
            byte[]? bytes = await cache.GetAsync(id);
            if (bytes == null)
            {
                return default;
            }

            Serializer serializer = new Serializer();
            serializer.Options.WithPreserveObjectReferences(true);
            await using MemoryStream mem = new MemoryStream(bytes);
            T? value = serializer.Deserialize<T>(mem);
            return value;
        }
        catch (Exception)
        {
            await cache.RemoveAsync(id);
            return default;
        }
    }
}
