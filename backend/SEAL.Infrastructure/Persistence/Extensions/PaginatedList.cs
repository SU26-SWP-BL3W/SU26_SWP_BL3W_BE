using Microsoft.EntityFrameworkCore;

namespace SEAL_Infrastructure.Persistence.Extensions
{
    public class PaginatedList<T>
    {
        public IEnumerable<T> Items { get; private set; }
        public int TotalItems { get; private set; }
        public int CurrentPage { get; private set; }
        public int TotalPages { get; private set; }
        public int PageSize { get; private set; }

        public PaginatedList(IEnumerable<T> items, int count, int index, int pageSize)
        {
            TotalItems = count;
            CurrentPage = index;
            PageSize = pageSize > 100 ? 100 : pageSize;
            TotalPages = (count == 0 || pageSize == 0) ? 0 : (int)Math.Ceiling(count / (double)pageSize);
            Items = items;
        }

        public PaginatedList(IEnumerable<T> items, int count)
        {
            TotalItems = count;
            CurrentPage = 1;
            PageSize = 0;
            TotalPages = 1;
            Items = items;
        }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public static async Task<PaginatedList<T>> GetDataAsync(IQueryable<T> source, int index, int pageSize)
        {
            int count = await source.CountAsync();
            IEnumerable<T> items = await source.Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<T>(items, count, index, pageSize);
        }

        public static async Task<PaginatedList<T>> GetDataAsync(IQueryable<T> source)
        {
            int count = await source.CountAsync();
            IEnumerable<T> items = await source.ToListAsync();
            return new PaginatedList<T>(items, count);
        }
    }
}
