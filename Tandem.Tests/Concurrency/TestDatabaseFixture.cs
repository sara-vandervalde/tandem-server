using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Tandem.Data;

namespace Tandem.Tests.Concurrency;

// A real PostgreSQL database for the concurrency tests: the version check (xmin) happens inside
// the database, so a mock cannot prove it. Same server as the API, but a separate database
// ("tandem_tests") - the development data is never touched.
// Created once for all the tests in the class (xUnit IClassFixture), with every migration and the Seed
public class TestDatabaseFixture
{
    private const string TestDatabaseName = "tandem_tests";

    public TestDatabaseFixture()
    {
        // Same sources as the API: User Secrets locally, an environment variable elsewhere
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<TestDatabaseFixture>()
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' is missing. Set it with dotnet user-secrets (see README).");

        ConnectionString = new NpgsqlConnectionStringBuilder(connectionString) { Database = TestDatabaseName }
            .ConnectionString;

        using var context = CreateContext();
        context.Database.Migrate();
    }

    public string ConnectionString { get; }

    // A new, independent DbContext - like two separate HTTP requests in the API
    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options);
}
