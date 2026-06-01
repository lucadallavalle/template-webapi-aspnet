using System.Net;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using WebApiTemplate.Infrastructure.Persistence;
using Xunit;

namespace WebApiTemplate.IntegrationTests.Customers.Controllers;

public class DeleteTests(AppWebApplicationFactory factory) : BaseTestClass(factory)
{
    [Fact]
    public async Task WithCustomerPresentDeletesCorrectly()
    {
        await _factory.ResetDatabase();
        const int id = 1278;

        await using var scope = _factory.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<
            IDbContextFactory<AppDbContext>
        >();
        await using var context = await contextFactory.CreateDbContextAsync();
        context.Customers.Add(new Core.Customers.Customer { Id = id });
        await context.SaveChangesAsync();

        using var client = _factory.CreateClient();
        using var response = await client.DeleteAsync($"/api/v1/customers/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var allCustomers = await context.Customers.ToListAsync();
        allCustomers.Should().BeEmpty();
    }
}
