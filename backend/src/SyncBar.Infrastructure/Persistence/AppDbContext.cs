using Microsoft.EntityFrameworkCore;
using SyncBar.Application.Abstractions.Tenancy;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Exceptions;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenantService? currentTenant = null)
    : DbContext(options), IUnitOfWork
{
    private readonly ICurrentTenantService? _currentTenant = currentTenant;
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branchs => Set<Branch>();
    public DbSet<UnitOfMeasure> UnitOfMeasures => Set<UnitOfMeasure>();
    public DbSet<TableStatus> TableStatuses => Set<TableStatus>();
    public DbSet<ComandaStatus> ComandaStatuses => Set<ComandaStatus>();
    public DbSet<OrderStatus> OrderStatuses => Set<OrderStatus>();
    public DbSet<OrderItemStatus> OrderItemStatuses => Set<OrderItemStatus>();
    public DbSet<CashSessionStatus> CashSessionStatuses => Set<CashSessionStatus>();
    public DbSet<CashMovementType> CashMovementTypes => Set<CashMovementType>();
    public DbSet<StockMovementType> StockMovementTypes => Set<StockMovementType>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<JobTitle> JobTitles => Set<JobTitle>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AccessLog> AccessLogs => Set<AccessLog>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<DiningTable> DiningTables => Set<DiningTable>();
    public DbSet<TableReservation> TableReservations => Set<TableReservation>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Comanda> Comandas => Set<Comanda>();
    public DbSet<CustomerOrder> CustomerOrders => Set<CustomerOrder>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemComplement> OrderItemComplements => Set<OrderItemComplement>();
    public DbSet<CashRegister> CashRegisters => Set<CashRegister>();
    public DbSet<CashSession> CashSessions => Set<CashSession>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SalePayment> SalePayments => Set<SalePayment>();
    public DbSet<CashMovement> CashMovements => Set<CashMovement>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<AppFeature> AppFeatures => Set<AppFeature>();
    public DbSet<CostType> CostTypes => Set<CostType>();
    public DbSet<OperatingCost> OperatingCosts => Set<OperatingCost>();
    public DbSet<RevenueTarget> RevenueTargets => Set<RevenueTarget>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<Printer> Printers => Set<Printer>();
    public DbSet<PrinterSetting> PrinterSettings => Set<PrinterSetting>();
    public DbSet<OrderPartialPayment> OrderPartialPayments => Set<OrderPartialPayment>();
    public DbSet<ComandaSetting> ComandaSettings => Set<ComandaSetting>();
    public DbSet<ServiceFeeSetting> ServiceFeeSettings => Set<ServiceFeeSetting>();
    public DbSet<IFoodIntegrationSetting> IFoodIntegrationSettings => Set<IFoodIntegrationSetting>();
    public DbSet<IFoodMerchantMapping> IFoodMerchantMappings => Set<IFoodMerchantMapping>();
    public DbSet<IFoodOrder> IFoodOrders => Set<IFoodOrder>();
    public DbSet<IFoodLogisticsDelivery> IFoodLogisticsDeliveries => Set<IFoodLogisticsDelivery>();
    public DbSet<IFoodShippingDelivery> IFoodShippingDeliveries => Set<IFoodShippingDelivery>();
    public DbSet<IFoodCategoryMapping> IFoodCategoryMappings => Set<IFoodCategoryMapping>();
    public DbSet<IFoodProductMapping> IFoodProductMappings => Set<IFoodProductMapping>();
    public DbSet<IFoodFinancialEvent> IFoodFinancialEvents => Set<IFoodFinancialEvent>();
    public DbSet<IFoodSettlement> IFoodSettlements => Set<IFoodSettlement>();
    public DbSet<IFoodOpeningHours> IFoodOpeningHours => Set<IFoodOpeningHours>();
    public DbSet<ComplementItem> ComplementItems => Set<ComplementItem>();
    public DbSet<ComplementGroup> ComplementGroups => Set<ComplementGroup>();
    public DbSet<Complement> Complements => Set<Complement>();
    public DbSet<ProductComplementGroup> ProductComplementGroups => Set<ProductComplementGroup>();
    public DbSet<IFoodComplementGroupMapping> IFoodComplementGroupMappings => Set<IFoodComplementGroupMapping>();
    public DbSet<IFoodComplementMapping> IFoodComplementMappings => Set<IFoodComplementMapping>();
    public DbSet<JobTitleFeature> JobTitleFeatures => Set<JobTitleFeature>();
    public DbSet<AppUserFeature> AppUserFeatures => Set<AppUserFeature>();
    public DbSet<LogTracker> LogTrackers { get; set; }
    public DbSet<PizzaFlavor> PizzaFlavors => Set<PizzaFlavor>();
    public DbSet<PizzaConfiguration> PizzaConfigurations => Set<PizzaConfiguration>();
    public DbSet<PizzaSize> PizzaSizes => Set<PizzaSize>();
    public DbSet<PizzaCrust> PizzaCrusts => Set<PizzaCrust>();
    public DbSet<PizzaEdge> PizzaEdges => Set<PizzaEdge>();
    public DbSet<PizzaFlavorPrice> PizzaFlavorPrices => Set<PizzaFlavorPrice>();
    public DbSet<OrderItemPizzaFlavor> OrderItemPizzaFlavors => Set<OrderItemPizzaFlavor>();
    public DbSet<IFoodPizzaMapping> IFoodPizzaMappings => Set<IFoodPizzaMapping>();
    public DbSet<IFoodPizzaElementMapping> IFoodPizzaElementMappings => Set<IFoodPizzaElementMapping>();
    public DbSet<ComandaItemTransfer> ComandaItemTransfers => Set<ComandaItemTransfer>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ConfigureCompanyScopedTenantFilters(modelBuilder);
        ConfigureBranchScopedTenantFilters(modelBuilder);
    }

    private void ConfigureCompanyScopedTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Branch>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || e.CompanyId == _currentTenant.CompanyId);
        modelBuilder.Entity<AppUser>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || e.CompanyId == _currentTenant.CompanyId);
        modelBuilder.Entity<Category>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || e.CompanyId == _currentTenant.CompanyId);
        modelBuilder.Entity<JobTitle>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || e.CompanyId == _currentTenant.CompanyId);
        modelBuilder.Entity<Product>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || e.CompanyId == _currentTenant.CompanyId);
        modelBuilder.Entity<Role>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || e.CompanyId == _currentTenant.CompanyId);
        modelBuilder.Entity<Supplier>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || e.CompanyId == _currentTenant.CompanyId);
        modelBuilder.Entity<IFoodIntegrationSetting>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || e.CompanyId == _currentTenant.CompanyId);
        modelBuilder.Entity<ComplementItem>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || e.CompanyId == _currentTenant.CompanyId);
        modelBuilder.Entity<ComplementGroup>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || e.CompanyId == _currentTenant.CompanyId);
        modelBuilder.Entity<PizzaFlavor>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || e.CompanyId == _currentTenant.CompanyId);
    }

    private void ConfigureBranchScopedTenantFilters(ModelBuilder modelBuilder)
    {
        ConfigureBranchScopedTenantFiltersPart1(modelBuilder);
        ConfigureBranchScopedTenantFiltersPart2(modelBuilder);
        ConfigureBranchScopedTenantFiltersPart3(modelBuilder);
        ConfigureBranchScopedTenantFiltersPart4(modelBuilder);
    }

    private void ConfigureBranchScopedTenantFiltersPart1(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DiningTable>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<CustomerOrder>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<TableReservation>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<Employee>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<Purchase>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<ServiceFeeSetting>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<IFoodMerchantMapping>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
    }

    private void ConfigureBranchScopedTenantFiltersPart2(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IFoodOrder>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<IFoodLogisticsDelivery>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<IFoodShippingDelivery>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<IFoodCategoryMapping>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<IFoodProductMapping>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<IFoodFinancialEvent>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<IFoodSettlement>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
    }

    private void ConfigureBranchScopedTenantFiltersPart3(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IFoodOpeningHours>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<IFoodComplementGroupMapping>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<IFoodComplementMapping>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<IFoodPizzaMapping>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<ComandaSetting>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<Sale>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<Printer>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
    }

    private void ConfigureBranchScopedTenantFiltersPart4(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PrinterSetting>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<Promotion>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<RevenueTarget>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<OperatingCost>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<StockItem>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<Comanda>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<CashRegister>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException("Ocorreu um conflito de concorrência ao persistir as alterações.", ex);
        }
    }
}
