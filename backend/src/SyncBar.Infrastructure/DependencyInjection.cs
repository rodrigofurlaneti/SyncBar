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

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenProvider, JwtTokenProvider>();

        // Pagamento (Pix/gateway) e fiscal (NFC-e): implementação fake por padrão —
        // trocar por um provider real (ex.: MercadoPago, Focus NFe) quando houver credenciais.
        services.AddScoped<SyncBar.Application.Abstractions.Payments.IPaymentGatewayService, FakePaymentGatewayService>();
        services.AddScoped<SyncBar.Application.Abstractions.Fiscal.IFiscalDocumentService, FakeFiscalDocumentService>();

        // Integração iFood: cliente HTTP real (autenticação OAuth2), endpoint/payload confirmados
        // contra a doc oficial em 2026-08-19 — ver comentário em IFoodAuthClient. O segredo é
        // criptografado com Data Protection;
        // por padrão as chaves ficam no disco local (%LOCALAPPDATA%\ASP.NET\DataProtection-Keys
        // no Windows) — isso é OK para uma instância única, mas se a API rodar em mais de uma
        // máquina/instância no futuro, configure um key ring persistente e compartilhado
        // (ex.: PersistKeysToDbContext ou um blob storage) — senão cada instância descriptografa
        // só os segredos que ela mesma cifrou.
        services.AddDataProtection();
        services.AddSingleton<SyncBar.Application.Abstractions.Security.ISecretProtector, DataProtectionSecretProtector>();
        services.AddHttpClient<SyncBar.Application.Abstractions.Integrations.IFood.IIFoodAuthClient, IFoodAuthClient>(
            client => client.Timeout = TimeSpan.FromSeconds(15));

        // Sincronização de pedidos (fase 2, "fluxo essencial"): cliente HTTP do módulo Order,
        // cache de access token em memória (necessário agora — o polling roda a cada 30s e pedir
        // token novo toda vez não escala) e o loop de polling em si (BackgroundService,
        // singleton — cria seu próprio scope de DI a cada ciclo). Endpoints/formatos confirmados
        // contra a doc oficial em 2026-08-19 — ver comentário em IFoodOrderClient.
        services.AddMemoryCache();
        services.AddScoped<SyncBar.Application.Abstractions.Integrations.IFood.IIFoodTokenProvider, IFoodTokenProvider>();
        services.AddHttpClient<SyncBar.Application.Abstractions.Integrations.IFood.IIFoodOrderClient, IFoodOrderClient>(
            client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddHostedService<IFoodOrderPollingBackgroundService>();

        return services;
    }
}
