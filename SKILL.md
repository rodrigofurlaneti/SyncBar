---
name: syncbar-architect
description: Padrões de arquitetura e desenvolvimento do projeto SyncBar — sistema de gestão de bar e restaurante (.NET 9 + EF Core + SQL Server, Clean Architecture/DDD/CQRS). Use esta skill SEMPRE que trabalhar no projeto SyncBar em qualquer camada — criar features, entidades, handlers, controllers, repositórios, migrations, scripts SQL, seeds, testes — ou quando o usuário mencionar SyncBar, bar/restaurante, pedidos em mesa, comanda, caixa, estoque, faturamento, BarRestauranteDb, ou pastas do repositório SyncBar, mesmo que não peça "arquitetura" explicitamente.
---

# SyncBar — Padrões de Arquitetura e Desenvolvimento

## Propósito

Você é um arquiteto fullstack especialista neste projeto. Quando ativado, aplique consistentemente todos os padrões documentados aqui ao criar features, revisar código ou fazer scaffold. **Nunca altere features já homologadas** — apenas adicione novas seguindo os mesmos padrões.

---

## Stack Tecnológico

| Camada | Tecnologia | Versão |
|---|---|---|
| Runtime | .NET 9 / C# 13 | 9.0 |
| Web API | ASP.NET Core Web API | 9.0 |
| ORM | EF Core SQL Server | 9.0.5 |
| Mensageria | MediatR | 12.4.1 |
| Validação | FluentValidation | 11.11.0 |
| Auth | JWT Bearer + BCrypt (workFactor 12) | 9.0.5 / 4.0.3 |
| Testes unitários | xUnit + FluentAssertions + NSubstitute | 2.9.2 / 6.12.2 / 5.3.0 |
| Testes BDD | Reqnroll (Gherkin) + Moq | 2.4.1 / 4.20.72 |
| Testes arquitetura | NetArchTest.Rules | 1.3.2 |
| Banco de dados | SQL Server (T-SQL, PascalCase) — `BarRestauranteDb` | — |

---

## 1. Estrutura da Solução

```
SyncBar/
├── sql/
│   ├── BarRestaurante_DDL.sql          # DDL completo — 36 tabelas + índices (idempotente)
│   ├── BarRestaurante_Seed.sql         # Lookups, permissões, empresa/filial exemplo (idempotente)
│   ├── BarRestaurante_DiagramaER.mermaid
│   └── BarRestaurante_Modelagem.md     # Documento de modelagem
├── BackEnd/
│   └── SyncBar/
│       ├── SyncBar.Domain           # Zero dependências externas
│       ├── SyncBar.Application      # MediatR + FluentValidation
│       ├── SyncBar.Infrastructure   # EF Core + Repositórios
│       ├── SyncBar.API              # JWT + Swagger + Controllers
│       ├── SyncBar.Tests            # xUnit + FluentAssertions + NSubstitute
│       ├── SyncBar.Specs            # Reqnroll + Moq
│       └── SyncBar.ArchTests        # NetArchTest
```

**Regra de dependência (Clean Architecture):**
```
API → Application → Domain
Infrastructure implementa interfaces do Domain
```
Domain: **zero** dependências externas (sem MediatR, EF Core, FluentValidation).

---

## 2. Banco de Dados — `BarRestauranteDb`

Esquema completo em `sql/BarRestaurante_DDL.sql`; detalhes e regras em `sql/BarRestaurante_Modelagem.md`. Consulte-os antes de criar qualquer entidade — **não invente colunas**.

### 2.1 Módulos e Tabelas (36)

```
Organizacional : Company → Branch
Autenticação   : AppUser, Role, Permission, RolePermission, UserRole, RefreshToken, AccessLog
Funcionários   : JobTitle, Employee
Cardápio       : Category, Product, UnitOfMeasure
Mesas/Comandas : DiningTable (TableStatus), Comanda (ComandaStatus)
Pedidos        : CustomerOrder (OrderStatus) → OrderItem (OrderItemStatus)
Caixa          : CashRegister → CashSession (CashSessionStatus) → CashMovement (CashMovementType)
Faturamento    : Sale → SalePayment (PaymentMethod)
Estoque        : Supplier, Purchase → PurchaseItem, StockItem, StockMovement (StockMovementType)
```

Relações centrais:
- Cadastros apontam para `Company`; transações apontam para `Branch` (isolamento por filial).
- `CustomerOrder` aponta para `DiningTable` **ou** `Comanda` — `CK_CustomerOrder_Origin` exige pelo menos uma.
- `Sale` fecha um `CustomerOrder` (1:1 via `UQ_Sale_CustomerOrderId` filtrado) dentro de uma `CashSession`.
- `StockItem` é o saldo por filial × produto; `StockMovement` é o livro-razão de entrada/saída.

### 2.2 Nomes que evitam palavras reservadas

| Conceito | Tabela / Entidade | Motivo |
|---|---|---|
| Usuário | `AppUser` | `USER` é reservado no T-SQL |
| Pedido | `CustomerOrder` | `ORDER` é reservado no T-SQL |
| Mesa | `DiningTable` | `TABLE` é reservado no T-SQL |

### 2.3 Lookups Seedados (Ids fixos — usar como enums no C#)

```
OrderStatus       : 1 Aberto, 2 EmAndamento, 3 AguardandoPagamento, 4 Pago, 5 Cancelado
OrderItemStatus   : 1 Lançado, 2 EnviadoCozinha, 3 EmPreparo, 4 Pronto, 5 Entregue, 6 Cancelado
TableStatus       : 1 Livre, 2 Ocupada, 3 Reservada, 4 EmFechamento, 5 Interditada
ComandaStatus     : 1 Disponível, 2 EmUso, 3 Extraviada, 4 Bloqueada
CashSessionStatus : 1 Aberto, 2 Fechado, 3 Conferido
CashMovementType  : 1 Suprimento(+), 2 Sangria(−), 3 RecebimentoVenda(+), 4 EstornoVenda(−), 5 Despesa(−)
StockMovementType : 1 EntradaCompra(+), 2 SaidaVenda(−), 3 AjusteEntrada(+), 4 AjusteSaida(−),
                    5 Perda(−), 6 Quebra(−), 7 TransfEntrada(+), 8 TransfSaida(−),
                    9 DevolucaoFornecedor(−), 10 ConsumoInterno(−)
PaymentMethod     : 1 Dinheiro (AllowsChange=1), 2 CartaoCredito, 3 CartaoDebito, 4 Pix,
                    5 ValeRefeicao, 6 ValeAlimentacao, 7 Cortesia
```

### 2.4 Índices Críticos

```sql
-- Unicidade compatível com soft delete: índices únicos FILTRADOS por IsActive = 1
UQ_AppUser_UserName / UQ_AppUser_Email          (AppUser)
UQ_Employee_Cpf                                 (Employee)
UQ_Company_Cnpj                                 (Company)
UQ_DiningTable_BranchId_Number                  (mesa única por filial)
UQ_Comanda_BranchId_Code                        (cartão único por filial)
UQ_StockItem_BranchId_ProductId                 (um saldo por filial×produto)
UQ_Sale_CustomerOrderId                         (1 venda ativa por pedido — permite reemissão pós-cancelamento)
UQ_Sale_BranchId_SaleNumber                     (numeração sequencial por filial)

-- Consultas por período
IX_CustomerOrder_OpenedAt / IX_Sale_SoldAt / IX_StockMovement_MovedAt
```

### 2.5 Regras SQL

- **PascalCase** em tudo: tabelas, colunas, índices, FKs.
- **BIGINT IDENTITY(1,1)** para todas as PKs.
- **DATETIME2** para todas as datas (nunca DATETIME).
- **DECIMAL(18,2)** para valores monetários; **DECIMAL(18,3)** para quantidades de estoque (venda fracionada: kg, litro, dose).
- **CONVERT(DATETIME2, 'yyyy-mm-ddThh:mm:ss', 126)** para datas em seeds/scripts.
- **Soft delete:** nunca DELETE físico — sempre `UPDATE IsActive = 0`.
- **Datas inválidas** (ano < 1753 — limite do SQL Server datetime): gravar como NULL.
- **`SET IDENTITY_INSERT <tabela> ON`** com `BEGIN TRY SET IDENTITY_INSERT <tabela> OFF END TRY BEGIN CATCH END CATCH` antes de cada bloco de inserção controlada.
- **`DBCC CHECKIDENT ('tabela', RESEED, 0)`** em scripts de reset/seed.
- Toda tabela tem `CreatedAt` (default `SYSDATETIME()`), `UpdatedAt NULL`, `IsActive BIT`.
- Toda FK tem índice `IX_<Tabela>_<Coluna>` correspondente.

### 2.6 Entidades C# × Banco

| Entidade C# | Tipo | Aggregate Root? |
|---|---|---|
| `Company`, `Branch` | `AggregateRoot` | ✅ |
| `AppUser`, `Role` | `AggregateRoot` | ✅ |
| `Employee`, `JobTitle` | `AggregateRoot` | ✅ |
| `Product`, `Category`, `Supplier` | `AggregateRoot` | ✅ |
| `DiningTable`, `Comanda` | `AggregateRoot` | ✅ |
| `CustomerOrder` | `AggregateRoot` | ✅ |
| `CashRegister`, `CashSession` | `AggregateRoot` | ✅ |
| `Sale`, `Purchase`, `StockItem` | `AggregateRoot` | ✅ |
| `OrderItem`, `SalePayment`, `CashMovement`, `StockMovement`, `PurchaseItem` | `Entity` | — filhas do aggregate |
| `RolePermission`, `UserRole`, `RefreshToken`, `AccessLog` | `Entity` | — |
| Lookups (`OrderStatus`, `PaymentMethod`, …) | `Entity` (somente leitura) | — |

---

## 3. Domain Layer

### 3.1 Classes Base (Primitivos)

```csharp
// Entity — Id como long (BIGINT)
public abstract class Entity : IEquatable<Entity>
{
    public long Id { get; protected set; }
    protected Entity(long id) => Id = id;
    // Equals/GetHashCode/== por Id e tipo
}

// AggregateRoot — possui Domain Events
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];
    protected AggregateRoot(long id) : base(id) { }
    public IReadOnlyList<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}

// IDomainEvent — marcador puro SEM MediatR no Domain
public interface IDomainEvent { }

// ValueObject
public abstract class ValueObject : IEquatable<ValueObject>
{
    public abstract IEnumerable<object> GetAtomicValues();
}

// Error — record imutável
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}

// Result / Result<T>
Result.Success()            // Result
Result.Success<T>(value)    // Result<T>
Result.Failure(error)       // Result
Result.Failure<T>(error)    // Result<T>
```

### 3.2 Padrão de Entidade SyncBar

```csharp
public sealed class CustomerOrder : AggregateRoot
{
    public long BranchId { get; private set; }
    public long? DiningTableId { get; private set; }   // nullable — pedido pode ser só por comanda
    public long? ComandaId { get; private set; }
    public long EmployeeId { get; private set; }
    public long OrderStatusId { get; private set; }
    public DateTime OpenedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public decimal SubtotalAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal ServiceFeeAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private CustomerOrder() : base(0) { }   // EF Core — OBRIGATÓRIO

    private CustomerOrder(long branchId, long? diningTableId, long? comandaId, long employeeId) : base(0) { ... }

    public static Result<CustomerOrder> Create(long branchId, long? diningTableId, long? comandaId, long employeeId)
    {
        if (diningTableId is null && comandaId is null)
            return Result.Failure<CustomerOrder>(
                new Error("CustomerOrder.MissingOrigin", "Order must have a table or a comanda."));
        return Result.Success(new CustomerOrder(branchId, diningTableId, comandaId, employeeId));
    }

    public Result Close(decimal serviceFeeRate) { ... }   // calcula totais + taxa de serviço
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
}
```

**Regras fixas do Domain:**
- Classe `sealed`.
- Propriedades com `private set` — **nunca** `public set`.
- Strings não anuláveis com `= null!` (resolve CS8618 do EF Core).
- Strings anuláveis com `string?` sem inicializador.
- Construtor `private` vazio com `base(0)` para EF Core.
- Construtor `private` com parâmetros para a factory.
- Factory `static Result<T> Create(...)`.
- Métodos de mudança de estado retornam `Result` (ex.: `Close`, `AddItem`, `Cancel`).
- `Deactivate()` atualiza `IsActive = false` + `UpdatedAt`.
- `Id` é `long` (BIGINT), não `int`.

### 3.3 Invariantes de Negócio no Domain

```
CustomerOrder : precisa de DiningTableId OU ComandaId (espelha CK_CustomerOrder_Origin)
OrderItem     : Quantity > 0; UnitPrice congelado no lançamento (não recalcular do Product)
Sale          : TotalAmount = Subtotal − Discount + ServiceFee; soma dos SalePayment cobre o total
SalePayment   : Amount > 0; ChangeAmount só quando PaymentMethod.AllowsChange (Dinheiro)
CashSession   : uma sessão Aberta por CashRegister; fechamento grava Expected/Closing/Difference
CashMovement  : Amount > 0 — o sinal vem de CashMovementType.IsInflow
StockMovement : Quantity > 0 — o sinal vem de StockMovementType.IsInflow;
                todo ajuste de StockItem.CurrentQuantity DEVE gerar um StockMovement
AppUser       : senha BCrypt (workFactor 12); lockout via FailedAccessCount/LockoutEndAt
Datas         : ano < 1753 → gravar NULL (limite do SQL Server)
```

### 3.4 Interfaces de Repositório (Domain/Repositories/)

```csharp
// Padrão para todos os aggregates — exemplos principais:

ICustomerOrderRepository
  GetByIdAsync(long id, CancellationToken ct)
  GetOpenByBranchAsync(long branchId, CancellationToken ct)
  GetOpenByDiningTableAsync(long diningTableId, CancellationToken ct)
  GetOpenByComandaAsync(long comandaId, CancellationToken ct)
  AddAsync(CustomerOrder entity, CancellationToken ct)

ISaleRepository
  GetByIdAsync / GetByCashSessionAsync
  GetNextSaleNumberAsync(long branchId, CancellationToken ct)
  ExistsActiveByOrderAsync(long customerOrderId, CancellationToken ct)   -- espelha UQ_Sale_CustomerOrderId
  AddAsync

IStockItemRepository
  GetByIdAsync / GetByBranchAndProductAsync(long branchId, long productId, ...)
  GetBelowMinimumAsync(long branchId, CancellationToken ct)              -- alerta de reposição
  AddAsync

ICashSessionRepository
  GetByIdAsync / GetOpenByCashRegisterAsync(long cashRegisterId, ...)
  AddAsync

IAppUserRepository
  GetByIdAsync / GetByUserNameAsync(string userName, ...)
  ExistsAsync(string userName, string email, CancellationToken ct)
  AddAsync

IUnitOfWork
  CommitAsync(CancellationToken ct) → Task<int>
```

---

## 4. Application Layer — CQRS

### 4.1 Interfaces de Messaging (Application/Abstractions/Messaging/)

```csharp
public interface ICommand : IRequest<Result> { }
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }
public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand { }
public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse> { }
public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }
public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse> { }
```

> **SEMPRE** adicionar `using SyncBar.Application.Abstractions.Messaging;` em todo Command/Query.

### 4.2 Estrutura de Features (CRUD padrão)

```
Application/Features/{Aggregate}/
  Create/
    CreateXxxCommand.cs            ← sealed record : ICommand<long>
    CreateXxxCommandHandler.cs     ← internal sealed class : ICommandHandler<Cmd, long>
    CreateXxxCommandValidator.cs   ← sealed class : AbstractValidator<Cmd>
  Update/
    UpdateXxxCommand.cs            ← sealed record : ICommand
    UpdateXxxCommandHandler.cs     ← internal sealed class : ICommandHandler<Cmd>
    UpdateXxxCommandValidator.cs
  GetAll/
    GetAllXxxQuery.cs              ← sealed record : IQuery<IReadOnlyCollection<XxxResponse>>
    GetAllXxxQueryHandler.cs
  GetById/
    GetXxxByIdQuery.cs             ← sealed record : IQuery<XxxResponse>
    GetXxxByIdQueryHandler.cs
  XxxResponse.cs                   ← sealed record (DTO de leitura)
```

**Handlers são sempre `internal sealed class`** — nunca expostos fora da Application.

### 4.3 Features por Módulo (planejadas)

| Módulo | Commands | Queries |
|---|---|---|
| Auth | Login, RefreshToken, CreateUser, AssignRole | GetUsers, GetRoles/Permissions |
| Employees | Create, Update, Dismiss | GetAll, GetById |
| Orders | Open, AddItem, UpdateItemStatus, ApplyDiscount, Cancel, Close | GetOpenByBranch, GetByTable/Comanda, GetById |
| Cash | OpenSession, CloseSession, RegisterMovement | GetOpenSession, GetSessionSummary |
| Billing | RegisterSale (com pagamentos múltiplos), RefundSale | GetSalesBySession, GetBillingReport |
| Stock | RegisterPurchase, RegisterMovement, AdjustInventory | GetStockByBranch, GetBelowMinimum, GetMovementLedger |
| Catalog | CreateProduct, UpdateProduct, CreateCategory | GetMenu, GetById |

### 4.4 Padrão de Handler

```csharp
internal sealed class RegisterSaleCommandHandler(
    ISaleRepository saleRepository,
    ICustomerOrderRepository orderRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterSaleCommand, long>
{
    public async Task<Result<long>> Handle(RegisterSaleCommand request, CancellationToken cancellationToken)
    {
        // 1. Checar duplicata (venda ativa já existe para o pedido?)
        var exists = await saleRepository.ExistsActiveByOrderAsync(request.CustomerOrderId, cancellationToken);
        if (exists)
            return Result.Failure<long>(new Error("Sale.Duplicate", "Order already has an active sale."));

        // 2. Criar via factory do Domain
        var saleNumber = await saleRepository.GetNextSaleNumberAsync(request.BranchId, cancellationToken);
        var result = Sale.Create(request.BranchId, request.CustomerOrderId, request.CashSessionId, saleNumber, ...);
        if (result.IsFailure)
            return Result.Failure<long>(result.Error);

        // 3. Persistir e commitar
        await saleRepository.AddAsync(result.Value, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(result.Value.Id);
    }
}
```

### 4.5 FluentValidation

```csharp
public sealed class RegisterSaleCommandValidator : AbstractValidator<RegisterSaleCommand>
{
    public RegisterSaleCommandValidator()
    {
        RuleFor(x => x.CustomerOrderId).GreaterThan(0);
        RuleFor(x => x.Payments).NotEmpty().WithMessage("Sale requires at least one payment.");
        RuleForEach(x => x.Payments).ChildRules(p =>
        {
            p.RuleFor(x => x.Amount).GreaterThan(0);
            p.RuleFor(x => x.PaymentMethodId).GreaterThan(0);
        });
    }
}
```

Regra: validações de formato/tamanho → FluentValidation. Regras de negócio (pedido aberto? caixa aberto? soma dos pagamentos?) → entity factory ou handler.

---

## 5. Infrastructure Layer

### 5.1 AppDbContext

```csharp
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IUnitOfWork
{
    // Um DbSet por tabela — nomes idênticos às tabelas do DDL, ex.:
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<CustomerOrder> CustomerOrders => Set<CustomerOrder>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    // ... demais entidades

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    public async Task<int> CommitAsync(CancellationToken ct = default)
        => await SaveChangesAsync(ct);
}
```

`AppDbContext` implementa `IUnitOfWork` diretamente — registrado como `Scoped`.

### 5.2 EF Core — Regra de Tracking

```
❌ AsNoTracking() → NUNCA em métodos chamados para UPDATE (entidade precisa ser tracked)
✅ AsNoTracking() → SEMPRE em consultas puras (GetAll, GetById leitura, GetBy*)
```

### 5.3 EF Core — Padrão de Configuração

```csharp
internal sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sale");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();

        builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.SoldAt).HasColumnType("datetime2").IsRequired();

        // Índice único FILTRADO — 1 venda ativa por pedido (soft delete permite reemissão)
        builder.HasIndex(x => x.CustomerOrderId)
            .IsUnique()
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("UQ_Sale_CustomerOrderId");

        // FK entre aggregates → Restrict
        builder.HasOne<CustomerOrder>()
            .WithMany()
            .HasForeignKey(x => x.CustomerOrderId)
            .HasConstraintName("FK_Sale_CustomerOrder")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

**Regras de configuração:**
- `sealed`, implementa `IEntityTypeConfiguration<TEntity>`.
- Reside em `Infrastructure.Persistence.Configurations`.
- Nome termina com "Configuration".
- `ToTable` com o nome exato do DDL (singular: `Sale`, `CustomerOrder`, `OrderItem`…).
- `OnDelete(Cascade)` apenas para filhas do aggregate (OrderItem → CustomerOrder, SalePayment → Sale).
- `OnDelete(Restrict)` entre aggregates (Sale → CustomerOrder, StockMovement → StockItem).
- Quantidades de estoque: `decimal(18,3)`. Valores monetários: `decimal(18,2)`.
- Nunca usar `DateTime` — sempre `datetime2` no `HasColumnType`.
- Índices únicos do DDL replicados com `HasFilter("[IsActive] = 1")` e `HasDatabaseName`.

### 5.4 Padrão de Repositório Concreto

```csharp
internal sealed class StockItemRepository(AppDbContext context) : IStockItemRepository
{
    // Leitura — AsNoTracking obrigatório
    public async Task<StockItem?> GetByBranchAndProductAsync(long branchId, long productId, CancellationToken ct = default)
        => await context.StockItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.BranchId == branchId && x.ProductId == productId && x.IsActive, ct);

    // Alerta de reposição
    public async Task<IReadOnlyCollection<StockItem>> GetBelowMinimumAsync(long branchId, CancellationToken ct = default)
        => await context.StockItems.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive && x.CurrentQuantity < x.MinimumQuantity)
            .ToListAsync(ct);

    // Update — SEM AsNoTracking (precisa ser tracked)
    public async Task<StockItem?> GetForUpdateAsync(long id, CancellationToken ct = default)
        => await context.StockItems.FirstOrDefaultAsync(x => x.Id == id, ct);
}
```

- Classe `sealed`, termina com "Repository".
- Reside em `Infrastructure.Persistence.Repositories`.
- Ordenação **sempre em C#** após a query — nunca `ORDER BY` em SqlQuery (EF Core 9 envolve em subquery e SQL Server rejeita).

### 5.5 DependencyInjection.cs

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services, IConfiguration configuration)
{
    services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
            sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

    services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

    services.AddScoped<IAppUserRepository, AppUserRepository>();
    services.AddScoped<ICustomerOrderRepository, CustomerOrderRepository>();
    services.AddScoped<ISaleRepository, SaleRepository>();
    services.AddScoped<ICashSessionRepository, CashSessionRepository>();
    services.AddScoped<IStockItemRepository, StockItemRepository>();
    // ... um registro por repositório

    return services;
}
```

---

## 6. API Layer

### 6.1 ApiController Base

```csharp
[ApiController]
[Route("api/[controller]")]
public abstract class ApiController(IMediator mediator) : ControllerBase
{
    protected readonly IMediator Mediator = mediator;

    protected IActionResult HandleFailure(Result result)
        => result.Error.Code switch
        {
            var c when c.EndsWith(".NotFound")      => NotFound(CreateProblemDetails(result)),
            var c when c.EndsWith(".AlreadyExists") => Conflict(CreateProblemDetails(result)),
            var c when c.EndsWith(".Duplicate")     => Conflict(CreateProblemDetails(result)),
            _                                       => BadRequest(CreateProblemDetails(result))
        };

    private static ProblemDetails CreateProblemDetails(Result result)
        => new() { Title = result.Error.Code, Detail = result.Error.Message };
}
```

### 6.2 Padrão de Controller

```csharp
[Authorize]
public sealed class OrdersController(IMediator mediator) : ApiController(mediator)
{
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetOrderByIdQuery(id), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Open([FromBody] OpenOrderCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure
            ? HandleFailure(result)
            : CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }

    [HttpPost("{id:long}/items")]
    public async Task<IActionResult> AddItem(long id, [FromBody] AddOrderItemRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new AddOrderItemCommand(id, request.ProductId, request.Quantity, request.Notes), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }
}

// Request separado do Command quando há parâmetro de rota
public sealed record AddOrderItemRequest(long ProductId, decimal Quantity, string? Notes);
```

**Regras:**
- `sealed`, herda `ApiController`, tem `[Authorize]` (exceto `AuthController.Login`).
- Termina com "Controller" e reside em `SyncBar.API.Controllers`.
- Parâmetro de rota `{id:long}` (não `{id:int}` — IDs são BIGINT).
- `CreatedAtAction(nameof(GetById), ...)` para retorno 201.
- Request bodies são records separados (`XxxRequest`) quando há parâmetro de rota.

### 6.3 Program.cs / appsettings.json

```csharp
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* Issuer, Audience, Lifetime, assinatura */ });
```

```json
{
  "ConnectionStrings": { "DefaultConnection": "Server=...;Database=BarRestauranteDb;..." },
  "Jwt": { "Secret": "...", "Issuer": "SyncBar", "Audience": "SyncBar.Client", "ExpiresInMinutes": 60 }
}
```

---

## 7. Camadas de Teste

### 7.1 SyncBar.Tests — Testes Unitários

xUnit + FluentAssertions + NSubstitute. Cobre Domain (entidades, Result) e Application (handlers).

```csharp
var repository = Substitute.For<ISaleRepository>();
var unitOfWork = Substitute.For<IUnitOfWork>();
repository.ExistsActiveByOrderAsync(10, Arg.Any<CancellationToken>()).Returns(false);

result.IsSuccess.Should().BeTrue();
await repository.Received(1).AddAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
```

### 7.2 SyncBar.Specs — BDD / Gherkin

Reqnroll + Moq. Feature files em `Features/`, step definitions em `StepDefinitions/`.

**Regras críticas de Reqnroll:**
- Parênteses em step patterns devem ser escapados: `\(texto\)`.
- Um step pattern só pode ter um binding em todo o projeto.

Cenários prioritários: abrir pedido sem mesa nem comanda (falha), fechar pedido com taxa de serviço, venda com pagamento dividido, sangria acima do saldo do caixa, baixa de estoque abaixo do mínimo.

### 7.3 SyncBar.ArchTests — Testes de Arquitetura

NetArchTest.Rules 1.3.2. Fronteiras verificadas:

```
Domain → não depende de Application / Infrastructure / API / MediatR / EF Core / FluentValidation
Application → não depende de Infrastructure / API / EF Core
Infrastructure → não depende de API

Handlers          → internal sealed (CommandHandler, QueryHandler)
Repositories      → internal sealed (sufixo "Repository")
EF Configurations → internal sealed (sufixo "Configuration")
Response DTOs     → sealed (sufixo "Response")
Commands/Queries/Validators → residem em Application.Features
Validators        → herdam AbstractValidator<>
Aggregates (Company, Branch, AppUser, CustomerOrder, Sale, CashSession, StockItem, …) → herdam AggregateRoot
Filhas (OrderItem, SalePayment, CashMovement, StockMovement, PurchaseItem) → herdam Entity
Interfaces de repositório → Domain.Repositories | Concretos → Infrastructure.Persistence.Repositories
```

---

## 8. Regras Operacionais do Negócio

### 8.1 Fluxo Mesa/Comanda

```
Mesa: garçom abre CustomerOrder na DiningTable → TableStatus = Ocupada
Comanda: cliente recebe cartão → ComandaStatus = EmUso → CustomerOrder aponta ComandaId
Híbrido: comanda em mesa → CustomerOrder aponta AMBOS DiningTableId e ComandaId
Itens: OrderItem congela UnitPrice; status flui Lançado → EnviadoCozinha → EmPreparo → Pronto → Entregue
Fechamento: OrderStatus = AguardandoPagamento → Sale registrada → OrderStatus = Pago
           → TableStatus = Livre / ComandaStatus = Disponível
```

### 8.2 Caixa

```
Abertura: CashSession com OpeningAmount (fundo de troco); só UMA sessão Aberta por CashRegister
Operação: cada Sale vincula-se à CashSession aberta; Sangria/Suprimento viram CashMovement
Fechamento: ExpectedAmount (calculado) × ClosingAmount (contado) → DifferenceAmount
```

### 8.3 Estoque — Livro-Razão

```
StockItem.CurrentQuantity é saldo materializado; StockMovement é a fonte de verdade auditável.
TODA alteração de saldo gera StockMovement — nunca UPDATE direto sem movimento.
Compra: Purchase/PurchaseItem → StockMovement (EntradaCompra) com PurchaseItemId
Venda: fechamento do pedido → StockMovement (SaidaVenda) com OrderItemId, apenas se Product.IsStockControlled = 1
Pratos preparados (IsStockControlled = 0) não baixam estoque diretamente (ficha técnica é extensão futura)
```

### 8.4 Faturamento

```
SaleNumber: sequencial por Branch (GetNextSaleNumberAsync) — nunca IDENTITY global
Pagamento dividido: N SalePayment por Sale; soma deve cobrir TotalAmount; troco só em Dinheiro
Estorno: soft delete da Sale (IsActive = 0) + CashMovement (EstornoVenda) — índice filtrado permite nova Sale
```

---

## 9. Checklist — Nova Feature CRUD

- [ ] **Domain:** entidade `sealed`; `private set`; `= null!` para strings não anuláveis; construtor `private` vazio `base(0)`; factory `static Result<T> Create(...)` com validações de negócio; métodos de estado retornam `Result`; `Deactivate()`; interface `IXxxRepository` em `Domain/Repositories/`
- [ ] **EF Config:** `internal sealed`, `IEntityTypeConfiguration<T>`; `ToTable` com nome exato do DDL; `datetime2` para datas; `decimal(18,2)` valores / `decimal(18,3)` quantidades; índices únicos filtrados `[IsActive] = 1` com `HasDatabaseName`; `Cascade` para filhas, `Restrict` entre aggregates; `DbSet<T>` no `AppDbContext`
- [ ] **Repositório:** `internal sealed`, sufixo "Repository", em `Infrastructure/Persistence/Repositories/`; `AsNoTracking()` em todas as leituras; sem `AsNoTracking()` em métodos de update
- [ ] **DI:** `services.AddScoped<IXxxRepository, XxxRepository>()` em `DependencyInjection.cs`
- [ ] **Commands:** `using SyncBar.Application.Abstractions.Messaging;`; handler `internal sealed`; `await unitOfWork.CommitAsync(ct)`; validator `sealed : AbstractValidator<>`
- [ ] **Queries:** `AsNoTracking()`; `XxxResponse.cs` como `sealed record`; ordenação em C# (não no SQL)
- [ ] **Controller:** `sealed`, herda `ApiController`, `[Authorize]`; rota `{id:long}`; `CreatedAtAction(nameof(GetById), ...)`; request body separado do command quando há parâmetro de rota
- [ ] **SQL:** se a feature exigir tabela nova, seguir `BarRestaurante_DDL.sql`: `BIGINT IDENTITY`, `DATETIME2`, `DECIMAL(18,2)`, `IsActive`/`CreatedAt`/`UpdatedAt`, FK nomeada + índice `IX_`, unicidade filtrada por `IsActive = 1`
- [ ] **Testes:** unit test com NSubstitute para handler; BDD feature file + step definition; testes de arquitetura passando

---

## 10. Restrições Absolutas

1. **Não alterar o esquema SQL** (`sql/BarRestaurante_DDL.sql`) sem confirmação explícita — apenas adicionar.
2. **Não alterar features já homologadas** — apenas adicionar novas.
3. **Nunca `ORDER BY` dentro de `SqlQuery<T>`** — EF Core 9 quebra com SQL Server (envolve em subquery, SQL Server rejeita ORDER BY sem TOP/OFFSET).
4. **Nunca DELETE físico** — sempre soft delete (`IsActive = 0`). Unicidade é garantida por índices filtrados.
5. **Nunca `AsNoTracking()` em repositório usado para update**.
6. **Nunca referenciar MediatR, EF Core ou FluentValidation no Domain** — viola o teste de arquitetura.
7. **Nunca `public set` em propriedades de entidade** — viola testes de arquitetura.
8. **Nunca criar entidade sem `sealed`** — viola testes de arquitetura.
9. **Nunca criar handler sem sufixo "CommandHandler" ou "QueryHandler"** — viola testes de arquitetura.
10. **Nunca verificar senha em SQL** — hash BCrypt sempre verificado em C# via `BCrypt.Net.BCrypt.Verify()`.
11. **Nunca usar `DateTime` (T-SQL) no banco** — sempre `DATETIME2`; no C#, `DateTime` com `HasColumnType("datetime2")`.
12. **Nunca usar `int` para IDs** — todos os IDs são `long` (BIGINT IDENTITY).
13. **Nunca alterar `StockItem.CurrentQuantity` sem gerar `StockMovement`** — o livro-razão é a fonte de verdade auditável.
14. **Nunca registrar `Sale` sem `CashSession` aberta** e nunca abrir segunda sessão no mesmo `CashRegister`.
15. **Nunca recalcular preço de `OrderItem` a partir do `Product`** — `UnitPrice` é congelado no lançamento.
