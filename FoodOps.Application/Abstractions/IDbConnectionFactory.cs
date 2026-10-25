using System.Data;

namespace FoodOps.Application.Abstractions;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}