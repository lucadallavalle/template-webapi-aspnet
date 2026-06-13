using System.Net;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using WebApiTemplate.Application.Customers;
using WebApiTemplate.Infrastructure.Persistence;
using Xunit;

namespace WebApiTemplate.IntegrationTests.Customers.Controllers;

public class GetByIdTests(AppWebApplicationFactory factory) : BaseTestClass(factory)
{
    [Fact]
    public async Task WithCustomerPresentInDbShouldReturnCorrectly()
    {
        await _factory.ResetDatabase();
        const int id = 23;

        await using var scope = _factory.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<
            IDbContextFactory<AppDbContext>
        >();
        await using var context = await contextFactory.CreateDbContextAsync();
        context.Customers.Add(new Core.Customers.Customer { Id = id });
        await context.SaveChangesAsync();

        using var client = _factory.CreateClient();
        using var response = await client.GetAsync($"/api/v1/customers/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var customer = await response.Content.ReadFromJsonAsync<CustomerDto>();
        customer.Should().NotBeNull();
        customer!.Id.Should().Be(id);
    }

    [Fact]
    public async Task WithNoCustomerPresentInDbShouldReturnNotFound()
    {
        await _factory.ResetDatabase();
        const int id = 483930;

        using var client = _factory.CreateClient();
        using var response = await client.GetAsync($"/api/v1/customers/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
