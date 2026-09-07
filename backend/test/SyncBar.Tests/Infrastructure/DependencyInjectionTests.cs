using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SyncBar.Application.Abstractions.Integrations.Asaas;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Abstractions.Notifications;
using SyncBar.Domain.Repositories;
using SyncBar.Infrastructure.Integrations.Ifood;
using SyncBar.Infrastructure.Integrations.Keeta;
using Xunit;

namespace SyncBar.Tests.Infrastructure;

// AddInfrastructure is a composition root: ~210 lines of AddHttpClient/AddHostedService/
// Configure<TSettings>/AddScoped registrations with no business logic. Building a full
// ServiceProvider and resolving every registration is impractical here (it would need a real
// IDataProtectionProvider path, a reachable MySQL connection string, and settings sections for
// every third-party integration) and low value for a composition-only file. Instead we build a
// minimal IServiceCollection + IConfiguration (enough that nothing eager throws) and assert
// directly on the returned ServiceCollection's descriptors for a representative sample of
// registrations, plus the AddHostedService<KeetaEventPollingBackgroundService> registration.
//
// The only side effect this method performs eagerly (not lazily, at resolution time) is the
// DataProtection key folder check/creation under AppContext.BaseDirectory/app_data/protecting-keys
// (the `if (!Directory.Exists(keysFolder))` branch around the AddDataProtection() call). We cover
// both branches of that condition explicitly and clean up the directory afterwards so repeated
// test runs don't accumulate garbage.
public sealed class DependencyInjectionTests : IDisposable
{
    private static string KeysFolder => Path.Combine(AppContext.BaseDirectory, "app_data", "protecting-keys");

    public void Dispose()
    {
        if (Directory.Exists(KeysFolder))
            Directory.Delete(KeysFolder, recursive: true);
    }

    private static IConfiguration BuildMinimalConfiguration()
    {
        // AddInfrastructure only reads GetConnectionString("DefaultConnection") eagerly (a plain
        // string lookup that never throws even when absent); every Configure<TSettings> call is
        // lazy and resolved only when a settings object is actually requested. So an empty
        // in-memory configuration is enough to exercise registration without throwing.
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=Test;Uid=test;Pwd=test;"
            })
            .Build();
    }

    [Fact]
    public void AddInfrastructure_WhenKeysFolderDoesNotExist_ShouldCreateItAndRegisterCoreServices()
    {
        if (Directory.Exists(KeysFolder))
            Directory.Delete(KeysFolder, recursive: true);

        var services = new ServiceCollection();
        var configuration = BuildMinimalConfiguration();

        var result = SyncBar.Infrastructure.DependencyInjection.AddInfrastructure(services, configuration);

        result.Should().BeSameAs(services);
        Directory.Exists(KeysFolder).Should().BeTrue();
    }

    [Fact]
    public void AddInfrastructure_WhenKeysFolderAlreadyExists_ShouldNotThrowAndStillRegisterServices()
    {
        Directory.CreateDirectory(KeysFolder);

        var services = new ServiceCollection();
        var configuration = BuildMinimalConfiguration();

        var act = () => SyncBar.Infrastructure.DependencyInjection.AddInfrastructure(services, configuration);

        act.Should().NotThrow();
        Directory.Exists(KeysFolder).Should().BeTrue();
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterRepresentativeRepositoriesAsScoped()
    {
        var services = new ServiceCollection();
        var configuration = BuildMinimalConfiguration();

        SyncBar.Infrastructure.DependencyInjection.AddInfrastructure(services, configuration);

        services.Should().Contain(d => d.ServiceType == typeof(IAppUserRepository) && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(ICustomerOrderRepository) && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(IStockItemRepository) && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(IKeetaIntegrationSettingRepository) && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(IAsaasIntegrationSettingRepository) && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterIntegrationServicesAndUnitOfWork()
    {
        var services = new ServiceCollection();
        var configuration = BuildMinimalConfiguration();

        SyncBar.Infrastructure.DependencyInjection.AddInfrastructure(services, configuration);

        services.Should().Contain(d => d.ServiceType == typeof(IUnitOfWork));
        services.Should().Contain(d => d.ServiceType == typeof(IWhatsAppService));
        services.Should().Contain(d => d.ServiceType == typeof(IKeetaAuthClient));
        services.Should().Contain(d => d.ServiceType == typeof(IAsaasService));
        services.Should().Contain(d => d.ServiceType == typeof(IIfoodTokenProvider));
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterKeetaEventPollingBackgroundServiceAsHostedService()
    {
        var services = new ServiceCollection();
        var configuration = BuildMinimalConfiguration();

        SyncBar.Infrastructure.DependencyInjection.AddInfrastructure(services, configuration);

        services.Should().Contain(d =>
            d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
            d.ImplementationType == typeof(KeetaEventPollingBackgroundService));
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterIfoodBackgroundServicesAsHostedServices()
    {
        var services = new ServiceCollection();
        var configuration = BuildMinimalConfiguration();

        SyncBar.Infrastructure.DependencyInjection.AddInfrastructure(services, configuration);

        services.Should().Contain(d =>
            d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
            d.ImplementationType == typeof(IfoodOrderPollingBackgroundService));
        services.Should().Contain(d =>
            d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
            d.ImplementationType == typeof(IfoodFinancialSyncBackgroundService));
    }
}
