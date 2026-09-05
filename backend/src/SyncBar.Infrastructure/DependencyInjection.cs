using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Abstractions.Tenancy;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using SyncBar.Infrastructure.Authentication;
using SyncBar.Infrastructure.Fiscal;
using SyncBar.Infrastructure.Integrations.Asaas;
using SyncBar.Infrastructure.Integrations.Ifood;
using SyncBar.Infrastructure.Payments;
using SyncBar.Infrastructure.Persistence;
using SyncBar.Infrastructure.Persistence.Repositories;
using SyncBar.Infrastructure.Persistence.Repositories;
using SyncBar.Infrastructure.Printing;
using SyncBar.Infrastructure.Security;
using SyncBar.Infrastructure.Storage;
using SyncBar.Infrastructure.Tenancy;
using System.IO;

namespace SyncBar.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(
                connectionString,
                new MySqlServerVersion(new Version(8, 4, 9)),
                mysql =>
                {
                    mysql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    mysql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
                }));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IDiningAreaRepository, DiningAreaRepository>();
        services.AddScoped<IDiningAreaTableRepository, DiningAreaTableRepository>();
        services.AddScoped<IDiningAreaAssignmentRepository, DiningAreaAssignmentRepository>();
        services.AddScoped<IAppUserRepository, AppUserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ICustomerOrderRepository, CustomerOrderRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IDiningTableRepository, DiningTableRepository>();
        services.AddScoped<IComandaRepository, ComandaRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<ICashSessionRepository, CashSessionRepository>();
        services.AddScoped<ICashMovementRepository, CashMovementRepository>();
        services.AddScoped<ICashRegisterRepository, CashRegisterRepository>();
        services.AddScoped<IShiftClosingRepository, ShiftClosingRepository>();
        services.AddScoped<IShiftClosingSessionRepository, ShiftClosingSessionRepository>();
        services.AddScoped<IStockItemRepository, StockItemRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IJobTitleRepository, JobTitleRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IAppFeatureRepository, AppFeatureRepository>();
        services.AddScoped<IJobTitleFeatureRepository, JobTitleFeatureRepository>();
        services.AddScoped<IAppUserFeatureRepository, AppUserFeatureRepository>();
        services.AddScoped<IOperatingCostRepository, OperatingCostRepository>();
        services.AddScoped<IRevenueTargetRepository, RevenueTargetRepository>();
        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<IPrinterRepository, PrinterRepository>();
        services.AddScoped<IPrinterSettingRepository, PrinterSettingRepository>();
        services.AddScoped<IOrderPartialPaymentRepository, OrderPartialPaymentRepository>();
        services.AddScoped<IComandaSettingRepository, ComandaSettingRepository>();
        services.AddScoped<IServiceFeeSettingRepository, ServiceFeeSettingRepository>();
        services.AddScoped<IIfoodIntegrationSettingRepository, IfoodIntegrationSettingRepository>();
        services.AddScoped<IIfoodMerchantMappingRepository, IfoodMerchantMappingRepository>();
        services.AddScoped<IIfoodOrderRepository, IfoodOrderRepository>();
        services.AddScoped<IIfoodLogisticsDeliveryRepository, IfoodLogisticsDeliveryRepository>();
        services.AddScoped<IIfoodShippingDeliveryRepository, IfoodShippingDeliveryRepository>();
        services.AddScoped<IIfoodCategoryMappingRepository, IfoodCategoryMappingRepository>();
        services.AddScoped<IIfoodProductMappingRepository, IfoodProductMappingRepository>();
        services.AddScoped<IIfoodFinancialEventRepository, IfoodFinancialEventRepository>();
        services.AddScoped<IIfoodSettlementRepository, IfoodSettlementRepository>();
        services.AddScoped<IIfoodOpeningHoursRepository, IfoodOpeningHoursRepository>();
        services.AddScoped<IComplementItemRepository, ComplementItemRepository>();
        services.AddScoped<IComplementGroupRepository, ComplementGroupRepository>();
        services.AddScoped<IProductComplementGroupRepository, ProductComplementGroupRepository>();
        services.AddScoped<IIfoodComplementGroupMappingRepository, IfoodComplementGroupMappingRepository>();
        services.AddScoped<IIfoodComplementMappingRepository, IfoodComplementMappingRepository>();
        services.AddScoped<IPizzaFlavorRepository, PizzaFlavorRepository>();
        services.AddScoped<IPizzaConfigurationRepository, PizzaConfigurationRepository>();
        services.AddScoped<IIfoodPizzaMappingRepository, IfoodPizzaMappingRepository>();
        services.AddScoped<IAccessLogRepository, AccessLogRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<ITableReservationRepository, TableReservationRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductStockRepository, ProductStockRepository>();
        services.AddScoped<ILogTrackerRepository, LogTrackerRepository>();
        services.AddScoped<ICustomerAppUserRepository, CustomerAppUserRepository>();
        services.AddScoped<ICustomerAddressRepository, CustomerAddressRepository>();
        services.AddScoped<IAsaasIntegrationCustomerRepository, AsaasIntegrationCustomerRepository>();
        services.AddScoped<IAsaasIntegrationPaymentRepository, AsaasIntegrationPaymentRepository>();
        services.AddScoped<IAsaasIntegrationSavedCardRepository, AsaasIntegrationSavedCardRepository>();
        services.AddScoped<IAsaasIntegrationSettingRepository, AsaasIntegrationSettingRepository>();
        services.AddScoped<IAsaasIntegrationWebhookLogRepository, AsaasIntegrationWebhookLogRepository>();

        services.AddSingleton<TimeProvider, SyncBar.Infrastructure.Time.TimeProviderCustom>();
        services.AddSingleton<SyncBar.Application.Abstractions.Storage.IImageStorage, LocalImageStorage>();
        services.AddSingleton<IRawPrinterTransport, WindowsRawPrinterTransport>();
        services.AddSingleton<IRawPrinterTransport, NetworkRawPrinterTransport>();
        services.AddScoped<SyncBar.Application.Abstractions.Printing.IPrintingService, PrintingService>();
        services.AddScoped<IWaiterMessageRepository, WaiterMessageRepository>();
        services.AddScoped<ITableItemTransferRepository, TableItemTransferRepository>();
        services.AddScoped<IComandaItemTransferRepository, ComandaItemTransferRepository>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenProvider, JwtTokenProvider>();
        services.AddScoped<SyncBar.Application.Abstractions.Payments.IPaymentGatewayService, FakePaymentGatewayService>();
        services.AddScoped<SyncBar.Application.Abstractions.Fiscal.IFiscalDocumentService, FakeFiscalDocumentService>();

        // -----------------------------------------------------------------
        // CONFIGURAÇÃO PERSISTENTE DO DATA PROTECTION (FIM DO ERRO DE CHAVE)
        // -----------------------------------------------------------------
        var keysFolder = Path.Combine(AppContext.BaseDirectory, "app_data", "protecting-keys");
        if (!Directory.Exists(keysFolder))
        {
            Directory.CreateDirectory(keysFolder);
        }

        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
            .SetApplicationName("SyncBar");
        // -----------------------------------------------------------------

        services.AddSingleton<SyncBar.Application.Abstractions.Security.ISecretProtector, DataProtectionSecretProtector>();

        services.AddHttpClient<SyncBar.Application.Abstractions.Integrations.Ifood.IIfoodAuthClient, IfoodAuthClient>(
            client => client.Timeout = TimeSpan.FromSeconds(15));

        services.AddMemoryCache();
        services.AddScoped<SyncBar.Application.Abstractions.Integrations.Ifood.IIfoodTokenProvider, IfoodTokenProvider>();

        services.AddHttpClient<SyncBar.Application.Abstractions.Integrations.Ifood.IIfoodOrderClient, IfoodOrderClient>(
            client => client.Timeout = TimeSpan.FromSeconds(15));

        services.AddHostedService<IfoodOrderPollingBackgroundService>();
        services.AddScoped<SyncBar.Application.Abstractions.Integrations.Ifood.IIfoodCatalogSyncTrigger, IfoodCatalogSyncTrigger>();

        services.AddHttpClient<SyncBar.Application.Abstractions.Integrations.Ifood.IIfoodCatalogClient, IfoodCatalogClient>(
            client => client.Timeout = TimeSpan.FromSeconds(15));

        services.AddHttpClient<SyncBar.Application.Abstractions.Integrations.Ifood.IIfoodFinancialClient, IfoodFinancialClient>(
            client => client.Timeout = TimeSpan.FromSeconds(30));

        services.AddHostedService<IfoodFinancialSyncBackgroundService>();

        services.AddHttpClient<SyncBar.Application.Abstractions.Integrations.Ifood.IIfoodMerchantClient, IfoodMerchantClient>(
            client => client.Timeout = TimeSpan.FromSeconds(15));

        services.AddSingleton<SyncBar.Application.Abstractions.Integrations.Ifood.IIfoodOperationalAlertStore, InMemoryIfoodOperationalAlertStore>();
        services.AddHostedService<IfoodMerchantStatusWatcherBackgroundService>();

        services.AddHttpClient<SyncBar.Application.Abstractions.Integrations.Ifood.IIfoodLogisticsClient, IfoodLogisticsClient>(
            client => client.Timeout = TimeSpan.FromSeconds(15));

        services.AddHttpClient<SyncBar.Application.Abstractions.Integrations.Ifood.IIfoodShippingClient, IfoodShippingClient>(
            client => client.Timeout = TimeSpan.FromSeconds(15));

        services.AddHttpClient<SyncBar.Application.Abstractions.Integrations.Ifood.IIfoodReviewClient, IfoodReviewClient>(
            client => client.Timeout = TimeSpan.FromSeconds(15));

        services.AddHostedService<IfoodReviewWatcherBackgroundService>();

        services.AddHttpClient<SyncBar.Application.Abstractions.Integrations.Ifood.IIfoodAnalyticsClient, IfoodAnalyticsClient>(
            client => client.Timeout = TimeSpan.FromSeconds(20));

        services.Configure<AsaasSettings>(builder.Configuration.GetSection("AsaasSettings"));

        services.AddHttpClient<AsaasAuthClient>();
        services.AddScoped<IAsaasService, AsaasService>();

        return services;
    }
}