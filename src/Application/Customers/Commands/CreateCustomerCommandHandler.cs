using HumbleMediator;
using WebApiTemplate.Core.Customers;
using WebApiTemplate.Core.Persistence;

namespace WebApiTemplate.Application.Customers.Commands;

/// <summary>
/// <see cref="ICommandHandler{TCommand,TCommandResult}"/> implementation for creating a new customer entity.
/// </summary>
/// <param name="uowFactory">The unit-of-work factory.</param>
public sealed class CreateCustomerCommandHandler(IUnitOfWorkFactory uowFactory)
    : ICommandHandler<CreateCustomerCommand, int>
{
    /// <summary>
    /// Handle the command to create a new customer entity.
    /// </summary>
    /// <param name="command">The <see cref="CreateCustomerCommand"/> to handle.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken" />.</param>
    /// <returns>The id of the newly-created entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the store did not assign an id.</exception>
    public Task<int> Handle(
        CreateCustomerCommand command,
        CancellationToken cancellationToken = default
    ) =>
        uowFactory.ExecuteInTransactionAsync<int>(
            async (uow, ct) =>
            {
                var customers = uow.GetRepository<ICustomerWriteRepository>();
                await customers.AddAsync(command.Customer, ct);

                // The id is store-generated; flush so it is populated before we return it.
                await uow.FlushAsync(ct);

                return command.Customer.Id
                    ?? throw new InvalidOperationException("New customer has no Id");
            },
            cancellationToken
        );
}
