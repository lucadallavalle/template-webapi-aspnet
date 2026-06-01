using AwesomeAssertions;
using NSubstitute;
using WebApiTemplate.Application.Customers.Queries;
using WebApiTemplate.Core.Customers;
using Xunit;

namespace WebApiTemplate.UnitTests.Customers.GetCustomerByIdQueryHandler;

public class HandleTests
{
    [Fact]
    public async Task WithValidRequestShouldCallRepository()
    {
        // Arrange
        var expected = new Customer { Id = 1 };
        var mock = Substitute.For<ICustomerReadRepository>();
        mock.GetById(default).ReturnsForAnyArgs(expected);

        var sut = new Application.Customers.Queries.GetCustomerByIdQueryHandler(mock);

        // Act
        var result = await sut.Handle(new GetCustomerByIdQuery(1));

        // Assert
        result.Should().BeEquivalentTo(expected);
    }
}
