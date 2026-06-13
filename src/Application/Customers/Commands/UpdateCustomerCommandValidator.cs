using FluentValidation;

namespace WebApiTemplate.Application.Customers.Commands;

/// <summary>
/// Validator for the <see cref="UpdateCustomerCommand"/> command.
/// </summary>
public sealed class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCustomerCommandValidator"/> class.
    /// </summary>
    public UpdateCustomerCommandValidator()
    {
        RuleFor(e => e.Id).GreaterThan(0);
    }
}
