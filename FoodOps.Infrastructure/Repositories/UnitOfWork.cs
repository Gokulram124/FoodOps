using FoodOps.Application.Interfaces;
using FoodOps.Domain.Entities;
using FoodOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;
using static FoodOps.Application.Interfaces.IUnitOfWork;

namespace FoodOps.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
        Orders = new GenericRepository<Order>(db);
        OrderStatusLogs = new GenericRepository<OrderStatusLog>(db);
        MenuItems = new GenericRepository<MenuItem>(db);
        Restaurants = new GenericRepository<Restaurant>(db);
    }

    public IGenericRepository<Order> Orders { get; }
    public IGenericRepository<OrderStatusLog> OrderStatusLogs { get; }
    public IGenericRepository<MenuItem> MenuItems { get; }
    public IGenericRepository<Restaurant> Restaurants { get; }

    public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync()
    => new EfTransaction(await _db.Database.BeginTransactionAsync());

    private sealed class EfTransaction : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _tx;
        public EfTransaction(IDbContextTransaction tx) => _tx = tx;
        public Task CommitAsync() => _tx.CommitAsync();
        public ValueTask DisposeAsync() => _tx.DisposeAsync();   // Commit pannama dispose = auto rollback
    }
}