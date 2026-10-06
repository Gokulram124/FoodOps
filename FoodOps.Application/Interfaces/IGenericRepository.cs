namespace FoodOps.Application.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    IQueryable<T> Query();             // tracked (use when you will modify)
    IQueryable<T> QueryNoTracking();   // read-only (faster)
    Task AddAsync(T entity);
    void Remove(T entity);
}