namespace FoodOps.Application.Common;

public record KeysetResult<T>(IReadOnlyList<T> Items, string? NextCursor, bool HasMore);