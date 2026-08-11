using Microsoft.EntityFrameworkCore.Storage;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Infrastructure.Persistence;
using SEAL_Infrastructure.Repositories;

namespace SEAL_Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DatabaseContext _context;
        private IDbContextTransaction? _transaction;
        private readonly Dictionary<Type, object> _repositories;

        public UnitOfWork(DatabaseContext context)
        {
            _context = context;
            _repositories = [];
        }



        // Trả về repository động và đảm bảo chỉ có một instance
        public IGenericRepository<T> GetRepository<T>() where T : class
        {
            var type = typeof(T);

            // Kiểm tra xem repository đã tồn tại chưa
            if (_repositories.ContainsKey(type))
            {
                return (IGenericRepository<T>)_repositories[type];
            }

            // Nếu chưa tồn tại, tạo mới và lưu vào Dictionary
            var repository = new GenericRepository<T>(_context);
            _repositories.Add(type, repository);

            return repository;
        }

        // Lưu thay đổi đồng bộ
        public void Save()
        {
            _context.SaveChanges();
        }

        // Lưu thay đổi bất đồng bộ
        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        // Bắt đầu giao dịch
        public void BeginTransaction()
        {
            _transaction = _context.Database.BeginTransaction();
        }

        // Commit giao dịch
        public void CommitTransaction()
        {
            if (_transaction != null)
            {
                _transaction.Commit();
                _transaction.Dispose();
                _transaction = null;
            }
        }

        // Rollback giao dịch
        public void RollBack()
        {
            if (_transaction != null)
            {
                _transaction.Rollback();
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }


        public void ClearTracker()
        {
            // Xóa sạch trạng thái của các thực thể cũ trong bộ nhớ
            _context.ChangeTracker.Clear();
        }

        // Dispose để giải phóng tài nguyên
        public void Dispose()
        {
            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null;
            }
            _context.Dispose();
        }
    }
}
