using FluentValidation;

namespace WebApiTemplate.Application.Customers.Commands;

/// <summary>
/// Validator for <see cref="DeleteCustomerCommand"/>.
/// </summary>
public sealed class DeleteCustomerCommandValidator : AbstractValidator<DeleteCustomerCommand>
{
    /// <summary>
    /// Defines the validation rules for the <see cref="DeleteCustomerCommand"/>.
    /// </summary>
    public DeleteCustomerCommandValidator()
    {
        RuleFor(e => e.Id).GreaterThan(0);
    }
}
