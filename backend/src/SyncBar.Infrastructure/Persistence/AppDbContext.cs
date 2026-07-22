using Microsoft.EntityFrameworkCore;
using SyncBar.Application.Abstractions.Tenancy;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenantService? currentTenant = null)
    : DbContext(options), IUnitOfWork
{
    // ICurrentTenantService é opcional para não quebrar cenários sem DI (design-time/migrations).
    // Fora de uma requisição HTTP autenticada, CompanyId é null e os filtros abaixo não restringem nada —
    // é responsabilidade de quem chama fora do pipeline HTTP (jobs, seeds) ter certeza do escopo.
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
    public DbSet<JobTitleFeature> JobTitleFeatures => Set<JobTitleFeature>();
    public DbSet<AppUserFeature> AppUserFeatures => Set<AppUserFeature>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // --- Isolamento multi-tenant (SaaS) ---
        // Entidades que carregam CompanyId diretamente: filtradas por igualdade simples.
        // Quando _currentTenant é null (sem HTTP context) ou CompanyId é null (sem usuário autenticado),
        // o filtro vira "true" e não restringe — use IgnoreQueryFilters() conscientemente em jobs internos.
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

        // Entidades escopadas por BranchId (não por CompanyId diretamente): filtra via
        // subquery em Branchs (que já tem seu próprio filtro por CompanyId — EF compõe os dois
        // como AND, redundante mas correto). Cobre as 16 entidades com BranchId direto.
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
        modelBuilder.Entity<ComandaSetting>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<Sale>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
        modelBuilder.Entity<Printer>().HasQueryFilter(e =>
            !_currentTenant!.CompanyId.HasValue || Branchs.Any(b => b.Id == e.BranchId && b.CompanyId == _currentTenant.CompanyId));
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

        // TODO (próximo fast-follow): entidades "netas" da filial — filhas de uma entidade já
        // filtrada por BranchId, mas sem BranchId próprio (StockMovement via StockItemId,
        // CashSession/CashMovement via CashRegisterId ou CashSessionId, SalePayment via SaleId,
        // OrderItem via CustomerOrderId, PurchaseItem via PurchaseId). Essas só ficam 100% isoladas
        // quando sempre acessadas através do pai já filtrado (Include) — repositórios que
        // consultam esses DbSets diretamente por Id ainda não têm o filtro em cascata.
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => await SaveChangesAsync(cancellationToken);
}
