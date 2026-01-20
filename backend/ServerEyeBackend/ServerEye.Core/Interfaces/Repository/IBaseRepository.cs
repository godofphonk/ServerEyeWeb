namespace ServerEye.Core.Interfaces.Repository;

public interface IBaseRepository<T>
    where T : class
{
    public Task<List<T>> GetAllAsync();
    public Task AddAsync(T entity);
    public Task DeleteAsync(Guid id);
    public Task<T?> GetByIdAsync(Guid id);
}
