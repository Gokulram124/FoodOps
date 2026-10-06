using FoodOps.Application.Interfaces;
using FoodOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodOps.Infrastructure.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    private readonly AppDbContext _db;
    public GenericRepository(AppDbContext db) => _db = db;

    public async Task<T?> GetByIdAsync(int id) => await _db.Set<T>().FindAsync(id);
    public IQueryable<T> Query() => _db.Set<T>();
    public IQueryable<T> QueryNoTracking() => _db.Set<T>().AsNoTracking();
    public async Task AddAsync(T entity) => await _db.Set<T>().AddAsync(entity);
    public void Remove(T entity) => _db.Set<T>().Remove(entity);
}