using FoodOps.Domain.Entities;

namespace FoodOps.Application.Interfaces;

public interface IUnitOfWork
{
    IGenericRepository<Order> Orders { get; }
    IGenericRepository<OrderStatusLog> OrderStatusLogs { get; }
    IGenericRepository<MenuItem> MenuItems { get; }
    IGenericRepository<Restaurant> Restaurants { get; }

    Task<int> SaveChangesAsync();
    public interface IUnitOfWorkTransaction : IAsyncDisposable
    {
        Task CommitAsync();
    }
    Task<IUnitOfWorkTransaction> BeginTransactionAsync();
}