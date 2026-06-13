using AwesomeAssertions;
using NSubstitute;
using WebApiTemplate.Application.Customers.Commands;
using WebApiTemplate.Core.Customers;
using WebApiTemplate.Core.Persistence;
using Xunit;

namespace WebApiTemplate.UnitTests.Customers.UpdateCustomerCommandHandler;

public class HandleTests
{
    [Fact]
    public async Task LoadsByRouteIdAndNeverStagesAnInsert()
    {
        // Arrange
        var repository = Substitute.For<ICustomerWriteRepository>();
        repository
            .GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new Customer { Id = 42 });

        var uow = Substitute.For<IUnitOfWork>();
        uow.GetRepository<ICustomerWriteRepository>().Returns(repository);

        var uowFactory = Substitute.For<IUnitOfWorkFactory>();
        uowFactory
            .ExecuteInTransactionAsync(
                Arg.Any<Func<IUnitOfWork, CancellationToken, Task>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(call =>
                call.Arg<Func<IUnitOfWork, CancellationToken, Task>>()(
                    uow,
                    call.Arg<CancellationToken>()
                )
            );

        var sut = new Application.Customers.Commands.UpdateCustomerCommandHandler(uowFactory);

        // Act — the command body carries no id; the route id (42) must be the one used.
        await sut.Handle(new UpdateCustomerCommand(42, new Customer()));

        // Assert: load-then-modify uses the route id ...
        await repository.Received(1).GetByIdAsync(42, Arg.Any<CancellationToken>());

        // ... and the handler never stages an insert or a detached update (the silent-insert bug).
        await repository
            .DidNotReceive()
            .AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
        await repository
            .DidNotReceive()
            .UpdateAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
    }
}
