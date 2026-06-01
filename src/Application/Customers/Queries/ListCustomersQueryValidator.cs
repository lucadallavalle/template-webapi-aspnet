using FluentValidation;

namespace WebApiTemplate.Application.Customers.Queries;

/// <summary>
/// Validates the <see cref="ListCustomersQuery"/> parameters.
/// </summary>
public sealed class ListCustomersQueryValidator : AbstractValidator<ListCustomersQuery>
{
    // Sortable fields for Customer. Add your entity's fields here, and the matching mapping
    // in CustomerReadRepository.GetSortSelector.
    private static readonly HashSet<string> AllowedSortFields = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        "id",
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ListCustomersQueryValidator"/> class.
    /// </summary>
    public ListCustomersQueryValidator()
    {
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);

        RuleFor(x => x.OrderBy)
            .Must(BeValidSortExpression!)
            .When(x => !string.IsNullOrWhiteSpace(x.OrderBy))
            .WithMessage("Invalid sort field. Allowed: id");
    }

    private static bool BeValidSortExpression(string orderBy)
    {
        var fields = orderBy.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        return fields.All(f =>
        {
            var fieldName = f.StartsWith('-') ? f[1..] : f;
            return AllowedSortFields.Contains(fieldName);
        });
    }
}
