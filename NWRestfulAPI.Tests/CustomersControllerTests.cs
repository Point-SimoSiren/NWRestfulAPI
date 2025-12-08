using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.DependencyInjection;
using NWRestfulAPI.Models;

namespace NWRestfulAPI.Tests;

public class CustomersControllerTests
{
    private static readonly WebApplicationFactoryClientOptions ClientOptions = new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    };

    [Fact]
    public async Task GetAllCustomers_ReturnsSeededCustomers()
    {
        await using var factory = new CustomersApiFactory();
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient(ClientOptions);

        var response = await client.GetAsync("/api/customers");

        response.EnsureSuccessStatusCode();
        var customers = await response.Content.ReadFromJsonAsync<List<Customer>>();
        Assert.NotNull(customers);
        Assert.Equal(2, customers.Count);
    }

    [Fact]
    public async Task GetCustomerOrders_ReturnsOrdersForExistingCustomer()
    {
        await using var factory = new CustomersApiFactory();
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient(ClientOptions);

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
        await using var factory = new CustomersApiFactory();
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient(ClientOptions);

        var response = await client.GetAsync("/api/customers/MISSING");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AddCustomer_CreatesNewCustomer()
    {
        await using var factory = new CustomersApiFactory();
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient(ClientOptions);

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
        await using var factory = new CustomersApiFactory();
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient(ClientOptions);

        var duplicate = new Customer { CustomerId = "DUP01", CompanyName = "Alfreds Futterkiste" };

        var response = await client.PostAsJsonAsync("/api/customers", duplicate);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EditCustomer_UpdatesExistingCustomer()
    {
        await using var factory = new CustomersApiFactory();
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient(ClientOptions);

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
        await using var factory = new CustomersApiFactory();
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient(ClientOptions);

        var response = await client.DeleteAsync("/api/customers/ANATR");

        response.EnsureSuccessStatusCode();

        var all = await client.GetFromJsonAsync<List<Customer>>("/api/customers");
        Assert.NotNull(all);
        Assert.DoesNotContain(all, c => c.CustomerId == "ANATR");
    }
}

public class CustomersApiFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? connection;
    private bool databaseInitialized;
    private readonly object dbLock = new();
    private static readonly Lazy<IModel> TestModel = new(BuildTestModel);

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

            services.AddSingleton<DbConnection>(_ =>
            {
                connection ??= new SqliteConnection("DataSource=:memory:");
                connection.Open();
                return connection;
            });

            services.AddScoped<NorthwindOriginalContext>(sp =>
            {
                var dbConnection = sp.GetRequiredService<DbConnection>();
                var optionsBuilder = new DbContextOptionsBuilder<NorthwindOriginalContext>();
                optionsBuilder.UseSqlite(dbConnection);
                optionsBuilder.UseModel(TestModel.Value);
                return new TestNorthwindContext(optionsBuilder.Options);
            });
        });
    }

    public Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NorthwindOriginalContext>();
        if (db is not TestNorthwindContext)
        {
            throw new InvalidOperationException("TestNorthwindContext was not resolved.");
        }
        lock (dbLock)
        {
            if (!databaseInitialized)
            {
                db.Database.EnsureCreated();
                databaseInitialized = true;
            }

            ClearData(db);
            Seed(db);
        }
        return Task.CompletedTask;
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

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            connection?.Dispose();
        }
    }

    private static void ClearData(NorthwindOriginalContext db)
    {
        db.Orders.RemoveRange(db.Orders);
        db.Customers.RemoveRange(db.Customers);
        db.SaveChanges();
    }

    private class TestNorthwindContext : NorthwindOriginalContext
    {
        public TestNorthwindContext(DbContextOptions<NorthwindOriginalContext> options) : base(options)
        {
        }
    }

    private static IModel BuildTestModel()
    {
        var conventionOptions = new DbContextOptionsBuilder().UseSqlite("DataSource=:memory:").Options;
        var conventions = ConventionSet.CreateConventionSet(new DbContext(conventionOptions));
        var modelBuilder = new ModelBuilder(conventions);

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(e => e.CustomerId);
            entity.Property(e => e.CustomerId).HasMaxLength(5);
            entity.Property(e => e.CompanyName).IsRequired();
            entity.Property(e => e.ContactName);
            entity.Property(e => e.ContactTitle);
            entity.Property(e => e.Address);
            entity.Property(e => e.City);
            entity.Property(e => e.Region);
            entity.Property(e => e.PostalCode);
            entity.Property(e => e.Country);
            entity.Property(e => e.Phone);
            entity.Property(e => e.Fax);
            entity.Ignore(e => e.CustomerTypes);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(e => e.OrderId);
            entity.Property(e => e.OrderId).ValueGeneratedNever();
            entity.Property(e => e.CustomerId).HasMaxLength(5);
            entity.Property(e => e.ShipName);
            entity.Ignore(e => e.OrderDetails);
            entity.Ignore(e => e.Employee);
            entity.Ignore(e => e.ShipViaNavigation);
            entity.HasOne(d => d.Customer)
                .WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        return (IModel)modelBuilder.Model;
    }
}
