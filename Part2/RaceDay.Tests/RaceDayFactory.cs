using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Tests;

/// <summary>
/// Starts the real API in memory for each test class, but swaps SQL Server for an
/// in-memory database so the tests are fast and need no database server (also works in GitHub Actions).
/// </summary>
public class RaceDayFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    /// <summary>The fake blob store used instead of Azure, so tests can inspect what was uploaded.</summary>
    public FakeBlobStorageService Blobs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<RaceDayDbContext>>();
            services.AddDbContext<RaceDayDbContext>(options => options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton<IBlobStorageService>(Blobs);
        });
    }
}
