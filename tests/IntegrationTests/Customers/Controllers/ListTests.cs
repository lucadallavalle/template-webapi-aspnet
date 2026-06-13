using System.Net;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using WebApiTemplate.Application.Customers;
using WebApiTemplate.Core.Customers;
using WebApiTemplate.Infrastructure.Persistence;
using WebApiTemplate.WebApi.Common;
using Xunit;

namespace WebApiTemplate.IntegrationTests.Customers.Controllers;

public class ListTests(AppWebApplicationFactory factory) : BaseTestClass(factory)
{
    [Fact]
    public async Task ReturnsPagedResult()
    {
        await _factory.ResetDatabase();

        await using (var scope = _factory.CreateScope())
        {
            var contextFactory = scope.ServiceProvider.GetRequiredService<
                IDbContextFactory<AppDbContext>
            >();
            await using var context = await contextFactory.CreateDbContextAsync();
            context.Customers.AddRange(new Customer(), new Customer(), new Customer());
            await context.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/customers?offset=0&limit=2&orderBy=id");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content.ReadFromJsonAsync<PagedResponse<CustomerDto>>();
        page.Should().NotBeNull();
        page!.Total.Should().Be(3);
        page.Items.Should().HaveCount(2);
    }
}
