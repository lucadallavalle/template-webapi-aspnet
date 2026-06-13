using HumbleMediator;
using WebApiTemplate.Core;
using WebApiTemplate.Core.Customers;
using WebApiTemplate.Core.Persistence;

namespace WebApiTemplate.Application.Customers.Commands;

/// <summary>
/// <see cref="ICommandHandler{TCommand,TCommandResult}"/> implementation for deleting a customer entity.
/// </summary>
/// <param name="uowFactory">The unit-of-work factory.</param>
public sealed class DeleteCustomerCommandHandler(IUnitOfWorkFactory uowFactory)
    : ICommandHandler<DeleteCustomerCommand, Nothing>
{
    /// <summary>
    /// Handle the command to delete a customer entity.
    /// </summary>
    /// <param name="command">The <see cref="DeleteCustomerCommand"/> to handle.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken" />.</param>
    /// <returns>An instance of the <see cref="Nothing" /> class.</returns>
    public async Task<Nothing> Handle(
        DeleteCustomerCommand command,
        CancellationToken cancellationToken = default
    )
    {
        await uowFactory.ExecuteInTransactionAsync(
            (uow, ct) =>
                uow.GetRepository<ICustomerWriteRepository>().DeleteByIdAsync(command.Id, ct),
            cancellationToken
        );

        return Nothing.Instance;
    }
}
