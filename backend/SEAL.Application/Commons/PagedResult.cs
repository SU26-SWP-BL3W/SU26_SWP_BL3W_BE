namespace SEAL_Application.Commons
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Data { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

        public PagedResult(IEnumerable<T> data, int pageNumber, int pageSize, int totalItems)
        {
            Data = data;
            CurrentPage = pageNumber;
            PageSize = pageSize;
            TotalItems = totalItems;
        }

        public bool HasPreviousPage => CurrentPage > 1;

        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
