using FluentValidation;

namespace WebApiTemplate.Application.Customers.Queries;

/// <summary>
/// Validator for the <see cref="GetCustomerByIdQuery"/> query.
/// </summary>
public sealed class GetCustomerByIdQueryValidator : AbstractValidator<GetCustomerByIdQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetCustomerByIdQueryValidator"/> class.
    /// </summary>
    public GetCustomerByIdQueryValidator()
    {
        RuleFor(e => e.Id).GreaterThan(0);
    }
}
