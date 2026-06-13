using System.Net;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using WebApiTemplate.Core.Customers;
using WebApiTemplate.Infrastructure.Persistence;
using WebApiTemplate.WebApi.Customers.Requests;
using Xunit;

namespace WebApiTemplate.IntegrationTests.Customers.Controllers;

public class UpdateTests(AppWebApplicationFactory factory) : BaseTestClass(factory)
{
    [Fact]
    public async Task WithExistingCustomerUpdatesInPlaceAndDoesNotInsert()
    {
        await _factory.ResetDatabase();
        const int id = 555;

        await using (var scope = _factory.CreateScope())
        {
            var contextFactory = scope.ServiceProvider.GetRequiredService<
                IDbContextFactory<AppDbContext>
            >();
            await using var context = await contextFactory.CreateDbContextAsync();
            context.Customers.Add(new Customer { Id = id });
            await context.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        using var response = await client.PutAsJsonAsync(
            $"/api/v1/customers/{id}",
            new UpdateCustomerRequest()
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using (var scope = _factory.CreateScope())
        {
            var contextFactory = scope.ServiceProvider.GetRequiredService<
                IDbContextFactory<AppDbContext>
            >();
            await using var context = await contextFactory.CreateDbContextAsync();
            var all = await context.Customers.ToListAsync();

            // The fix for the silent-insert bug: updating an existing row must UPDATE it, never
            // INSERT a second row. Exactly one row, still carrying the original id.
            all.Should().ContainSingle();
            all[0].Id.Should().Be(id);
        }
    }

    [Fact]
    public async Task WithMissingCustomerIsNoOpAndDoesNotInsert()
    {
        await _factory.ResetDatabase();
        const int absentId = 777;

        using var client = _factory.CreateClient();
        using var response = await client.PutAsJsonAsync(
            $"/api/v1/customers/{absentId}",
            new UpdateCustomerRequest()
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var scope = _factory.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<
            IDbContextFactory<AppDbContext>
        >();
        await using var context = await contextFactory.CreateDbContextAsync();

        // Updating a non-existent id is a no-op — it must not conjure a phantom row (the silent
        // insert that the old DbContext.Update(entity) path produced for key-less entities).
        var count = await context.Customers.CountAsync();
        count.Should().Be(0);
    }
}
