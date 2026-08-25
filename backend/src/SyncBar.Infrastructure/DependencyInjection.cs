using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Abstractions.Tenancy;
using SyncBar.Domain.Repositories;
using SyncBar.Infrastructure.Authentication;
using SyncBar.Infrastructure.Fiscal;
using SyncBar.Infrastructure.Integrations.IFood;
using SyncBar.Infrastructure.Payments;
using SyncBar.Infrastructure.Persistence;
using SyncBar.Infrastructure.Persistence.Repositories;
using SyncBar.Infrastructure.Printing;
using SyncBar.Infrastructure.Security;
using SyncBar.Infrastructure.Storage;
using SyncBar.Infrastructure.Tenancy;

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
        services.AddScoped<IIFoodIntegrationSettingRepository, IFoodIntegrationSettingRepository>();
        services.AddScoped<IIFoodMerchantMappingRepository, IFoodMerchantMappingRepository>();
        services.AddScoped<IIFoodOrderRepository, IFoodOrderRepository>();
        services.AddScoped<IIFoodLogisticsDeliveryRepository, IFoodLogisticsDeliveryRepository>();
        services.AddScoped<IIFoodShippingDeliveryRepository, IFoodShippingDeliveryRepository>();
        services.AddScoped<IIFoodCategoryMappingRepository, IFoodCategoryMappingRepository>();
        services.AddScoped<IIFoodProductMappingRepository, IFoodProductMappingRepository>();
        services.AddScoped<IIFoodFinancialEventRepository, IFoodFinancialEventRepository>();
        services.AddScoped<IIFoodSettlementRepository, IFoodSettlementRepository>();
        services.AddScoped<IIFoodOpeningHoursRepository, IFoodOpeningHoursRepository>();
        services.AddScoped<IComplementItemRepository, ComplementItemRepository>();
        services.AddScoped<IComplementGroupRepository, ComplementGroupRepository>();
        services.AddScoped<IProductComplementGroupRepository, ProductComplementGroupRepository>();
        services.AddScoped<IIFoodComplementGroupMappingRepository, IFoodComplementGroupMappingRepository>();
        services.AddScoped<IIFoodComplementMappingRepository, IFoodComplementMappingRepository>();
        services.AddScoped<IPizzaFlavorRepository, PizzaFlavorRepository>();
        services.AddScoped<IPizzaConfigurationRepository, PizzaConfigurationRepository>();
        services.AddScoped<IIFoodPizzaMappingRepository, IFoodPizzaMappingRepository>();
        services.AddScoped<IAccessLogRepository, AccessLogRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<ITableReservationRepository, TableReservationRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductStockRepository, ProductStockRepository>();
        services.AddScoped<ILogTrackerRepository, LogTrackerRepository>();
        services.AddScoped<IJobTitleRepository, JobTitleRepository>();
        services.AddSingleton<TimeProvider, SyncBar.Infrastructure.Time.TimeProviderCustom>();
        services.AddSingleton<SyncBar.Application.Abstractions.Storage.IImageStorage, LocalImageStorage>();
        services.AddSingleton<IRawPrinterTransport, WindowsRawPrinterTransport>();
        services.AddSingleton<IRawPrinterTransport, NetworkRawPrinterTransport>();
        services.AddScoped<SyncBar.Application.Abstractions.Printing.IPrintingService, PrintingService>();
        services.AddScoped<IWaiterMessageRepository, WaiterMessageRepository>();
        services.AddScoped<ITableItemTransferRepository, TableItemTransferRepository>();
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenProvider, JwtTokenProvider>();
        services.AddScoped<SyncBar.Application.Abstractions.Payments.IPaymentGatewayService, FakePaymentGatewayService>();
        services.AddScoped<SyncBar.Application.Abstractions.Fiscal.IFiscalDocumentService, FakeFiscalDocumentService>();
        services.AddDataProtection();
        services.AddSingleton<SyncBar.Application.Abstractions.Security.ISecretProtector, DataProtectionSecretProtector>();
        services.AddHttpClient<SyncBar.Application.Abstractions.Integrations.IFood.IIFoodAuthClient, IFoodAuthClient>(
            client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddMemoryCache();
        services.AddScoped<SyncBar.Application.Abstractions.Integrations.IFood.IIFoodTokenProvider, IFoodTokenProvider>();
        services.AddHttpClient<SyncBar.Application.Abstractions.Integrations.IFood.IIFoodOrderClient, IFoodOrderClient>(
            client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddHostedService<IFoodOrderPollingBackgroundService>();
        services.AddScoped<SyncBar.Application.Abstractions.Integrations.IFood.IIFoodCatalogSyncTrigger, IFoodCatalogSyncTrigger>();
        services.AddHttpClient<SyncBar.Application.Abstractions.Integrations.IFood.IIFoodCatalogClient, IFoodCatalogClient>(
            client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddHttpClient<SyncBar.Application.Abstractions.Integrations.IFood.IIFoodFinancialClient, IFoodFinancialClient>(
            client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddHostedService<IFoodFinancialSyncBackgroundService>();
        services.AddHttpClient<SyncBar.Application.Abstractions.Integrations.IFood.IIFoodMerchantClient, IFoodMerchantClient>(
            client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddSingleton<SyncBar.Application.Abstractions.Integrations.IFood.IIFoodOperationalAlertStore, InMemoryIFoodOperationalAlertStore>();
        services.AddHostedService<IFoodMerchantStatusWatcherBackgroundService>();
        services.AddHttpClient<SyncBar.Application.Abstractions.Integrations.IFood.IIFoodLogisticsClient, IFoodLogisticsClient>(
            client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddHttpClient<SyncBar.Application.Abstractions.Integrations.IFood.IIFoodShippingClient, IFoodShippingClient>(
            client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddHttpClient<SyncBar.Application.Abstractions.Integrations.IFood.IIFoodReviewClient, IFoodReviewClient>(
            client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddHostedService<IFoodReviewWatcherBackgroundService>();
        services.AddHttpClient<SyncBar.Application.Abstractions.Integrations.IFood.IIFoodAnalyticsClient, IFoodAnalyticsClient>(
            client => client.Timeout = TimeSpan.FromSeconds(20));

        return services;
    }
}
