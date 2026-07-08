using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IUnitOfWork
{
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
    public DbSet<Comanda> Comandas => Set<Comanda>();
    public DbSet<CustomerOrder> CustomerOrders => Set<CustomerOrder>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<CashRegister> CashRegisters => Set<CashRegister>();
    public DbSet<CashSession> CashSessions => Set<CashSession>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SalePayment> SalePayments => Set<SalePayment>();
    public DbSet<CashMovement> CashMovements => Set<CashMovement>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => await SaveChangesAsync(cancellationToken);
}
