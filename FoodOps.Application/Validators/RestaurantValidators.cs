using FluentValidation;
using FoodOps.Application.DTOs.Restaurants;

namespace FoodOps.Application.Validators;

public class CreateRestaurantValidator : AbstractValidator<CreateRestaurantDto>
{
    public CreateRestaurantValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Restaurant name is required.")
                            .MaximumLength(100).WithMessage("Name can be at most 100 characters.");
        RuleFor(x => x.City).NotEmpty().WithMessage("City is required.")
                            .MaximumLength(60).WithMessage("City can be at most 60 characters.");
    }
}

public class UpdateRestaurantValidator : AbstractValidator<UpdateRestaurantDto>
{
    public UpdateRestaurantValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Restaurant name is required.")
                            .MaximumLength(100).WithMessage("Name can be at most 100 characters.");
        RuleFor(x => x.City).NotEmpty().WithMessage("City is required.")
                            .MaximumLength(60).WithMessage("City can be at most 60 characters.");
    }
}

public class RestaurantQueryValidator : AbstractValidator<RestaurantQuery>
{
    public RestaurantQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Page must be 1 or more.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50).WithMessage("PageSize must be between 1 and 50.");
        RuleFor(x => x.MinRating).InclusiveBetween(0, 5).When(x => x.MinRating.HasValue)
                                 .WithMessage("MinRating must be between 0 and 5.");
        RuleFor(x => x.Filter).MaximumLength(200);
        RuleFor(x => x.Sort).MaximumLength(100);
    }
}