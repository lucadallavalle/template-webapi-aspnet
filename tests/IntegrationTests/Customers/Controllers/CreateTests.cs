using System.Net;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using WebApiTemplate.Infrastructure.Persistence;
using WebApiTemplate.WebApi.Customers.Requests;
using Xunit;

namespace WebApiTemplate.IntegrationTests.Customers.Controllers;

public class CreateTests(AppWebApplicationFactory factory) : BaseTestClass(factory)
{
    [Fact]
    public async Task WithValidRequestShouldCreateCorrectly()
    {
        await _factory.ResetDatabase();

        using var client = _factory.CreateClient();
        var request = new CreateCustomerRequest();
        using var response = await client.PostAsJsonAsync($"/api/v1/customers", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        await using var scope = _factory.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<
            IDbContextFactory<AppDbContext>
        >();
        await using var context = await contextFactory.CreateDbContextAsync();

        var result = await context.Customers.SingleOrDefaultAsync();
        result.Should().NotBeNull();
    }
}
