using NSubstitute;
using WebApiTemplate.Application.Customers.Commands;
using WebApiTemplate.Core;
using WebApiTemplate.Core.Customers;
using Xunit;

namespace WebApiTemplate.UnitTests.Customers.CreateCustomerCommandHandler;

public class HandleTests
{
    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP004:Don't ignore created IDisposable",
        Justification = "The IDisposable is not actually created in the test."
    )]
    public async Task WithValidRequestShouldCallRepository()
    {
        // Arrange
        var mockWriteRepository = Substitute.For<ICustomerWriteRepository>();
        mockWriteRepository
            .Create(Arg.Any<Customer>(), Arg.Any<IUnitOfWork>())
            .Returns(Nothing.Instance);

        var mockUow = Substitute.For<IUnitOfWork>();

        var mockUowFactory = Substitute.For<IUnitOfWorkFactory>();
        mockUowFactory.Create().ReturnsForAnyArgs(mockUow);

        var sut = new Application.Customers.Commands.CreateCustomerCommandHandler(
            mockUowFactory,
            mockWriteRepository
        );
        var newEntity = new Customer { Id = 13452 };

        // Act
        await sut.Handle(new CreateCustomerCommand(newEntity));

        // Assert
        await mockWriteRepository.Received(1).Create(Arg.Is(newEntity), Arg.Is(mockUow));
    }
}
