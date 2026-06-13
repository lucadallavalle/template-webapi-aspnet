using AwesomeAssertions;
using NSubstitute;
using WebApiTemplate.Application.Customers;
using WebApiTemplate.Application.Customers.Queries;
using WebApiTemplate.Core.Customers;
using Xunit;

namespace WebApiTemplate.UnitTests.Customers.GetCustomerByIdQueryHandler;

public class HandleTests
{
    [Fact]
    public async Task WithCustomerPresentMapsToDto()
    {
        // Arrange — the read repository is injected directly; no unit of work.
        var repository = Substitute.For<ICustomerReadRepository>();
        repository
            .GetById(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new Customer { Id = 1 });

        var sut = new Application.Customers.Queries.GetCustomerByIdQueryHandler(repository);

        // Act
        var result = await sut.Handle(new GetCustomerByIdQuery(1));

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        await repository.Received(1).GetById(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WithNoCustomerReturnsNull()
    {
        // Arrange
        var repository = Substitute.For<ICustomerReadRepository>();
        repository.GetById(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var sut = new Application.Customers.Queries.GetCustomerByIdQueryHandler(repository);

        // Act
        var result = await sut.Handle(new GetCustomerByIdQuery(999));

        // Assert
        result.Should().BeNull();
    }
}
