using System.Net;
using System.Net.Http.Json;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NWRestfulAPI.Models;

namespace NWRestfulAPI.Tests;

public class CustomersControllerTests : IClassFixture<CustomersApiFactory>
{
    private readonly CustomersApiFactory factory;
    private readonly HttpClient client;

    public CustomersControllerTests(CustomersApiFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetAllCustomers_ReturnsSeededCustomers()
    {
        await factory.ResetDatabaseAsync();

        var response = await client.GetAsync("/api/customers");

        response.EnsureSuccessStatusCode();
        var customers = await response.Content.ReadFromJsonAsync<List<Customer>>();
        Assert.NotNull(customers);
        Assert.Equal(2, customers.Count);
    }

    [Fact]
    public async Task GetCustomerOrders_ReturnsOrdersForExistingCustomer()
    {
        await factory.ResetDatabaseAsync();

        var response = await client.GetAsync("/api/customers/ALFKI");

        response.EnsureSuccessStatusCode();
        var orders = await response.Content.ReadFromJsonAsync<List<Order>>();
        Assert.NotNull(orders);
        var order = Assert.Single(orders);
        Assert.Equal("ALFKI", order.CustomerId);
    }

    [Fact]
    public async Task GetCustomerOrders_ReturnsNoContentWhenMissing()
    {
        await factory.ResetDatabaseAsync();

        var response = await client.GetAsync("/api/customers/MISSING");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AddCustomer_CreatesNewCustomer()
    {
        await factory.ResetDatabaseAsync();

        var newCustomer = new Customer { CustomerId = "NEW01", CompanyName = "New Company" };

        var response = await client.PostAsJsonAsync("/api/customers", newCustomer);

        response.EnsureSuccessStatusCode();

        var all = await client.GetFromJsonAsync<List<Customer>>("/api/customers");
        Assert.NotNull(all);
        Assert.Equal(3, all.Count);
        Assert.Contains(all, c => c.CustomerId == "NEW01" && c.CompanyName == "New Company");
    }

    [Fact]
    public async Task AddCustomer_ReturnsBadRequestForDuplicateCompany()
    {
        await factory.ResetDatabaseAsync();

        var duplicate = new Customer { CustomerId = "DUP01", CompanyName = "Alfreds Futterkiste" };

        var response = await client.PostAsJsonAsync("/api/customers", duplicate);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EditCustomer_UpdatesExistingCustomer()
    {
        await factory.ResetDatabaseAsync();

        var update = new Customer { CustomerId = "ALFKI", CompanyName = "Updated Name" };

        var response = await client.PutAsJsonAsync("/api/customers/ALFKI", update);

        response.EnsureSuccessStatusCode();

        var all = await client.GetFromJsonAsync<List<Customer>>("/api/customers");
        Assert.NotNull(all);
        Assert.Contains(all, c => c.CustomerId == "ALFKI" && c.CompanyName == "Updated Name");
    }

    [Fact]
    public async Task DeleteCustomer_RemovesCustomer()
    {
        await factory.ResetDatabaseAsync();

        var response = await client.DeleteAsync("/api/customers/ANATR");

        response.EnsureSuccessStatusCode();

        var all = await client.GetFromJsonAsync<List<Customer>>("/api/customers");
        Assert.NotNull(all);
        Assert.DoesNotContain(all, c => c.CustomerId == "ANATR");
    }
}

public class CustomersApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            var optionsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<NorthwindOriginalContext>));
            if (optionsDescriptor != null)
            {
                services.Remove(optionsDescriptor);
            }

            var contextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(NorthwindOriginalContext));
            if (contextDescriptor != null)
            {
                services.Remove(contextDescriptor);
            }

            services.AddDbContext<NorthwindOriginalContext>(options =>
            {
                options.UseInMemoryDatabase("CustomersApiTests");
            });

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NorthwindOriginalContext>();
            db.Database.EnsureCreated();
            Seed(db);
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NorthwindOriginalContext>();
        db.Customers.RemoveRange(db.Customers);
        db.Orders.RemoveRange(db.Orders);
        await db.SaveChangesAsync();
        Seed(db);
    }

    private static void Seed(NorthwindOriginalContext db)
    {
        db.Customers.RemoveRange(db.Customers);
        db.Orders.RemoveRange(db.Orders);
        db.SaveChanges();

        var alfreds = new Customer { CustomerId = "ALFKI", CompanyName = "Alfreds Futterkiste" };
        var ana = new Customer { CustomerId = "ANATR", CompanyName = "Ana Trujillo Emparedados y helados" };
        var order = new Order { OrderId = 1, CustomerId = alfreds.CustomerId, ShipName = "Test order" };

        alfreds.Orders.Add(order);

        db.Customers.AddRange(alfreds, ana);
        db.Orders.Add(order);
        db.SaveChanges();
    }
}
