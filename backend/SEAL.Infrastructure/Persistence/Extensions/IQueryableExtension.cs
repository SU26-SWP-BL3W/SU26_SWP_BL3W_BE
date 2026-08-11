using Microsoft.EntityFrameworkCore;

namespace SEAL_Infrastructure.Persistence.Extensions
{
    public static class IQueryableExtension
    {
        public static Task<PaginatedList<TSource>> GetPaginatedList<TSource>(this IQueryable<TSource> source) where TSource : class
            => PaginatedList<TSource>.GetDataAsync(source.AsNoTracking());
        public static Task<PaginatedList<TSource>> GetPaginatedList<TSource>(this IQueryable<TSource> source, int pageIndex, int pageSize) where TSource : class
            => PaginatedList<TSource>.GetDataAsync(source.AsNoTracking(), pageIndex, pageSize);
    }
}
