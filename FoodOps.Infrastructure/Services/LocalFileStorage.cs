using FoodOps.Application.Interfaces;

namespace FoodOps.Infrastructure.Services;

public class LocalFileStorage : IFileStorage
{
    private readonly string _root;
    public LocalFileStorage(string webRootPath) => _root = Path.GetFullPath(webRootPath);

    public async Task<string> SaveAsync(Stream content, string fileName, string folder, CancellationToken ct = default)
    {
        var dir = Path.Combine(_root, "uploads", folder);
        Directory.CreateDirectory(dir);
        await using var fs = File.Create(Path.Combine(dir, fileName));
        await content.CopyToAsync(fs, ct);
        return $"/uploads/{folder}/{fileName}";
    }

    public void Delete(string relativeUrl)
    {
        var path = Path.GetFullPath(Path.Combine(_root, relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        if (path.StartsWith(_root) && File.Exists(path)) File.Delete(path);   // never delete outside wwwroot
    }
}