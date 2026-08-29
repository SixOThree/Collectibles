using Collectibles.Infrastructure.Services;
using Collectibles.Web.Extensions;

using Hangfire;
using Hangfire.Common;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Collectibles.Application.Tests.Services;

public class HangfireServiceRegistrationTests
{
    [Fact]
    public void ConfigureServices_DoesNotRegisterHangfireSchemaInitializerAsHostedService()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=Collectibles-Hangfire-Tests;Trusted_Connection=True;MultipleActiveResultSets=true",
            })
            .Build();

        services.ConfigureServices(configuration);

        services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
            .Select(descriptor => descriptor.ImplementationType)
            .Should()
            .NotContain(typeof(HangfireSchemaInitializer));
    }

    [Fact]
    public async Task ConfigureHangfireRecurringJobsAsync_EnsuresSchemaBeforeRegisteringRecurringJobs()
    {
        var callOrder = new List<string>();
        var schemaInitializer = new Mock<IHangfireSchemaInitializer>();
        var recurringJobManager = new Mock<IRecurringJobManager>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:RequestLogRetentionDays"] = "365",
            })
            .Build();
        var services = new ServiceCollection();

        schemaInitializer
            .Setup(initializer => initializer.EnsureSchemaAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("schema"))
            .Returns(Task.CompletedTask);

        recurringJobManager
            .Setup(manager => manager.AddOrUpdate(
                It.IsAny<string>(),
                It.IsAny<Job>(),
                It.IsAny<string>(),
                It.IsAny<RecurringJobOptions>()))
            .Callback<string, Job, string, RecurringJobOptions>((recurringJobId, _, _, _) => callOrder.Add(recurringJobId));

        services.AddLogging();
        services.AddSingleton(schemaInitializer.Object);
        services.AddSingleton(recurringJobManager.Object);

        var app = new ApplicationBuilder(services.BuildServiceProvider());

        await app.ConfigureHangfireRecurringJobsAsync(configuration);

        // The point of this test is the ordering guarantee - the schema must exist before
        // any job is registered - not the current job count, which changes whenever a job
        // is added and previously broke this test for no real reason.
        callOrder.Should().NotBeEmpty();
        callOrder.First().Should().Be("schema");
        callOrder.Skip(1).Should().OnlyHaveUniqueItems();
        recurringJobManager.Verify(
            manager => manager.AddOrUpdate(
                It.IsAny<string>(),
                It.IsAny<Job>(),
                It.IsAny<string>(),
                It.IsAny<RecurringJobOptions>()),
            Times.AtLeastOnce);
    }
}
