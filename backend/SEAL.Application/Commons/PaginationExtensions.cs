using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Commons
{
    public static class PaginationExtensions
    {
        public static async Task<PagedResult<TResult>> ToPagedResultAsync<T, TResult>(
            this IQueryable<T> query,
            BasePaginationQuery request,
            Expression<Func<T, TResult>> selector,
            CancellationToken cancellationToken = default)
        {
            // 1. Áp dụng sorting (Dynamic Sorting bảo mật với Whitelist)
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                var allowedFields = request.GetAllowedSortFields();
                if (allowedFields.Contains(request.SortBy))
                {
                    var parameter = Expression.Parameter(typeof(T), "e");
                    try
                    {
                        var property = Expression.Property(parameter, request.SortBy);
                        var lambda = Expression.Lambda(property, parameter);

                        string methodName = request.IsAscending ? "OrderBy" : "OrderByDescending";
                        var resultExpression = Expression.Call(
                            typeof(Queryable),
                            methodName,
                            new Type[] { typeof(T), property.Type },
                            query.Expression,
                            Expression.Quote(lambda));

                        query = query.Provider.CreateQuery<T>(resultExpression);
                    }
                    catch (ArgumentException)
                    {
                        // Log hoặc bỏ qua nếu property không hợp lệ
                    }
                }
            }
            else
            {
                // Default sort để đảm bảo deterministic kết quả (thường dùng Id hoặc CreatedTime)
                // Giả định entity có thuộc tính Id. Nếu không, có thể điều chỉnh logic này.
                try 
                {
                    var parameter = Expression.Parameter(typeof(T), "e");
                    var property = Expression.Property(parameter, "Id");
                    var lambda = Expression.Lambda(property, parameter);
                    
                    var resultExpression = Expression.Call(
                        typeof(Queryable),
                        "OrderByDescending",
                        new Type[] { typeof(T), property.Type },
                        query.Expression,
                        Expression.Quote(lambda));
                        
                    query = query.Provider.CreateQuery<T>(resultExpression);
                }
                catch 
                {
                    // Nếu không có trường Id, giữ nguyên query gốc
                }
            }

            // 2. Thực hiện tuần tự để tránh lỗi concurrency trên Scoped DbContext
            var totalItems = await query.CountAsync(cancellationToken);
            var data = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(selector)
                .ToListAsync(cancellationToken);

            return new PagedResult<TResult>(
                data,
                request.PageNumber,
                request.PageSize,
                totalItems);
        }
    }
}
