using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SMS.Infrastructure.Persistence;

namespace SMS.Testing;

/// <summary>
/// Boots the real API pipeline while replacing SQL Server with an isolated database.
/// Every factory receives a unique store, which keeps local and CI test runs deterministic.
/// </summary>
public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"sms-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.UseSetting("Jwt:Secret", "integration-tests-only-signing-key-32-characters-minimum");
        builder.UseSetting("Jwt:Issuer", "SupportManagementSystem.Tests");
        builder.UseSetting("Jwt:Audience", "SupportManagementSystem.Tests");
        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            "Server=(local);Database=not-used-by-tests");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(local);Database=not-used-by-tests",
                ["Jwt:Secret"] = "integration-tests-only-signing-key-32-characters-minimum",
                ["Jwt:Key"] = "integration-tests-only-signing-key-32-characters-minimum",
                ["Jwt:Issuer"] = "SupportManagementSystem.Tests",
                ["Jwt:Audience"] = "SupportManagementSystem.Tests",
                ["Database:MigrateOnStartup"] = "true",
                ["Database:SeedDemoData"] = "true"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    public HttpClient CreateApiClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost")
    });
}
