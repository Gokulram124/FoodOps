using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FoodOps.API.Filters;

// Runs before every action: validates each action argument that has a FluentValidation validator.
public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _sp;
    public ValidationFilter(IServiceProvider sp) => _sp = sp;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var arg in context.ActionArguments.Values)
        {
            if (arg is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(arg.GetType());
            if (_sp.GetService(validatorType) is not IValidator validator) continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(arg));
            if (result.IsValid) continue;

            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            // "message" key = same shape as ExceptionMiddleware, so Angular shows it automatically
            context.Result = new BadRequestObjectResult(new
            {
                message = result.Errors[0].ErrorMessage,
                errors
            });
            return;
        }

        await next();
    }
}