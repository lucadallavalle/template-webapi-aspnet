using AwesomeAssertions;
using NSubstitute;
using WebApiTemplate.Application.Customers.Commands;
using WebApiTemplate.Core.Customers;
using WebApiTemplate.Core.Persistence;
using Xunit;

namespace WebApiTemplate.UnitTests.Customers.CreateCustomerCommandHandler;

public class HandleTests
{
    private const int ExpectedId = 13452;

    [Fact]
    public async Task AddsCustomerThenFlushesAndReturnsStoreGeneratedId()
    {
        // Arrange
        var repository = Substitute.For<ICustomerWriteRepository>();

        // Simulate the store assigning the identity key when the change is added/flushed.
        repository
            .When(r => r.AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>()))
            .Do(call => call.Arg<Customer>().Id = ExpectedId);

        var uow = Substitute.For<IUnitOfWork>();
        uow.GetRepository<ICustomerWriteRepository>().Returns(repository);

        // The factory invokes the captured delegate with our substitute unit of work.
        var uowFactory = Substitute.For<IUnitOfWorkFactory>();
        uowFactory
            .ExecuteInTransactionAsync(
                Arg.Any<Func<IUnitOfWork, CancellationToken, Task<int>>>(),
                Arg.Any<Func<CancellationToken, Task<CommitVerification<int>>>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(call =>
                call.Arg<Func<IUnitOfWork, CancellationToken, Task<int>>>()(
                    uow,
                    call.Arg<CancellationToken>()
                )
            );

        var sut = new Application.Customers.Commands.CreateCustomerCommandHandler(uowFactory);
        var customer = new Customer();

        // Act
        var result = await sut.Handle(new CreateCustomerCommand(customer));

        // Assert
        result.Should().Be(ExpectedId);
        await repository.Received(1).AddAsync(Arg.Is(customer), Arg.Any<CancellationToken>());
        await uow.Received(1).FlushAsync(Arg.Any<CancellationToken>());
    }
}
