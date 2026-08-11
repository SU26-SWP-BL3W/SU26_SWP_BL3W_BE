using SEAL_Application.Interfaces;

namespace SEAL_Application.Services.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<T> GetRepository<T>() where T : class;
        void Save();
        Task SaveAsync();
        void BeginTransaction();
        void CommitTransaction();
        void RollBack();

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        void ClearTracker();
    }
}
