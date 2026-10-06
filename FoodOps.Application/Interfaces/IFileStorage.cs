namespace FoodOps.Application.Interfaces;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string fileName, string folder, CancellationToken ct = default);
    void Delete(string relativeUrl);
}