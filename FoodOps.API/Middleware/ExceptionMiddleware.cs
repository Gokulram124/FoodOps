using FoodOps.Application.Common;

namespace FoodOps.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            var (status, message) = ex switch
            {
                NotFoundException => (404, ex.Message),
                ForbiddenException => (403, ex.Message),
                InvalidOperationException => (400, ex.Message),
                UnauthorizedAccessException => (401, ex.Message),
                _ => (500, "An unexpected error occurred.")
            };

            if (status == 500) _logger.LogError(ex, "Unhandled exception on {Path}", ctx.Request.Path);

            ctx.Response.StatusCode = status;
            await ctx.Response.WriteAsJsonAsync(new { message });
        }
    }
}