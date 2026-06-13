using HumbleMediator;
using WebApiTemplate.Core;
using WebApiTemplate.Core.Customers;
using WebApiTemplate.Core.Persistence;

namespace WebApiTemplate.Application.Customers.Commands;

/// <summary>
/// <see cref="ICommandHandler{TCommand,TCommandResult}"/> implementation for updating a customer entity.
/// </summary>
/// <param name="uowFactory">The unit-of-work factory.</param>
public sealed class UpdateCustomerCommandHandler(IUnitOfWorkFactory uowFactory)
    : ICommandHandler<UpdateCustomerCommand, Nothing>
{
    /// <summary>
    /// Handle the command to update a customer entity.
    /// </summary>
    /// <param name="command">The <see cref="UpdateCustomerCommand"/> to handle.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken" />.</param>
    /// <returns>An instance of the <see cref="Nothing" /> class.</returns>
    public async Task<Nothing> Handle(
        UpdateCustomerCommand command,
        CancellationToken cancellationToken = default
    )
    {
        await uowFactory.ExecuteInTransactionAsync(
            async (uow, ct) =>
            {
                var customers = uow.GetRepository<ICustomerWriteRepository>();

                // Load-then-modify: fetch the tracked entity by its route id and mutate it. Because
                // the entity is tracked, EF emits an UPDATE on commit and can never silently INSERT a
                // new row from a detached, key-less entity.
                var customer = await customers.GetByIdAsync(command.Id, ct);
                if (customer is null)
                {
                    // Nothing to update; the controller reports this as 204 No Content.
                    return;
                }

                // Copy the updated fields from the request onto the tracked entity here. Customer only
                // carries an Id in this template; a real entity would assign its fields, e.g.:
                //   customer.Name = command.Customer.Name;
            },
            cancellationToken
        );

        return Nothing.Instance;
    }
}
