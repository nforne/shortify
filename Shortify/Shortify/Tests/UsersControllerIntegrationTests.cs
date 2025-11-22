using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shortify.Data;
using Shortify.Repositories;
using Xunit;

namespace Shortify.Tests
{
    public class UsersControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public UsersControllerIntegrationTests(WebApplicationFactory<Program> factory) => _factory = factory;

        [Fact]
        public async Task GetAll_ReturnsSeededUsers()
        {
            // Arrange: create a factory that uses an in-memory database to keep tests isolated
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing ApplicationDbContext registration (if any) and replace with in-memory
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ShortifyDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<ShortifyDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("ShortifyTestDb_GetAll_ReturnsSeededUsers");
                    });

                    // Ensure the DB is created and seeded inside the test host
                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ShortifyDbContext>();
                    db.Database.EnsureDeleted();
                    db.Database.EnsureCreated();

                    // Run the same deterministic seeder used by the app
                    //DbSeeder.SeedAsync(db).GetAwaiter().GetResult();
                });
            });

            using var client = factory.CreateClient();

            // Act: call the endpoint
            var requestUri = "/api/users?tenantId=tenant_abc123&page=1&pageSize=50";
            // include test api key header to match minimal auth context expectations
            client.DefaultRequestHeaders.Add("x-api-key", "sk_test_abc123");

            var resp = await client.GetAsync(requestUri);

            // Assert: status 200 and seeded users present
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var body = await resp.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrWhiteSpace(body));

            // Try to parse the body as JSON array and validate seeded IDs/emails
            using var doc = JsonDocument.Parse(body);
            Assert.True(doc.RootElement.ValueKind == JsonValueKind.Array);

            // Expect at least the three seeded users
            var emails = doc.RootElement.EnumerateArray()
                .Select(e => e.GetProperty("email").GetString())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            Assert.Contains("admin@shortify.local", emails);
            Assert.Contains("alice@shortify.local", emails);
            Assert.Contains("bob@shortify.local", emails);
        }
    }
}
