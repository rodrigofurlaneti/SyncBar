using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Fase 17 — liga uma PizzaConfiguration do SyncBar à pizza correspondente no catálogo v1 (legado)
// do iFood, por FILIAL — mesmo padrão de IFoodProductMapping/IFoodCategoryMapping (o catálogo do
// iFood é por merchant). IFoodPizzaId é o id retornado pelo iFood na criação (POST
// merchants/{merchantId}/pizzas) — diferente dos outros mapeamentos, aqui o id não é gerado por
// nós porque a API de pizza do v1 não aceita um id proposto no create, só devolve um. Os ids de
// cada size/crust/edge/topping dentro dessa pizza (também devolvidos pelo iFood na criação, um
// por elemento) ficam nos IFoodPizzaElementMapping filhos — necessários pra todo PUT/PATCH
// seguinte referenciar os elementos certos (idempotência).
public sealed class IFoodPizzaMapping : AggregateRoot
{
    private readonly List<IFoodPizzaElementMapping> _elements = [];

    public long PizzaConfigurationId { get; private set; }
    public long BranchId { get; private set; }
    public string IFoodPizzaId { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }
    public IReadOnlyCollection<IFoodPizzaElementMapping> Elements => _elements.AsReadOnly();

    private IFoodPizzaMapping() : base(0) { }

    private IFoodPizzaMapping(long pizzaConfigurationId, long branchId, string ifoodPizzaId) : base(0)
    {
        PizzaConfigurationId = pizzaConfigurationId;
        BranchId = branchId;
        IFoodPizzaId = ifoodPizzaId;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<IFoodPizzaMapping> Create(long pizzaConfigurationId, long branchId, string ifoodPizzaId)
    {
        if (string.IsNullOrWhiteSpace(ifoodPizzaId))
            return Result.Failure<IFoodPizzaMapping>(new Error("IFoodPizzaMapping.EmptyId", "iFood pizza id is required."));

        return Result.Success(new IFoodPizzaMapping(pizzaConfigurationId, branchId, ifoodPizzaId));
    }

    // Upsert: se já existe um mapeamento ativo pra esse (kind, localId), atualiza o id do iFood;
    // senão, cria. `kind` usa as constantes em IFoodPizzaElementKind (Size/Crust/Edge/Topping).
    public IFoodPizzaElementMapping SetElement(byte kind, long localId, string ifoodElementId)
    {
        var existing = _elements.FirstOrDefault(e => e.IsActive && e.Kind == kind && e.LocalId == localId);
        if (existing is not null)
        {
            existing.UpdateIFoodElementId(ifoodElementId);
            UpdatedAt = DateTime.Now;
            return existing;
        }

        var created = IFoodPizzaElementMapping.Create(Id, kind, localId, ifoodElementId);
        _elements.Add(created);
        UpdatedAt = DateTime.Now;
        return created;
    }

    public string? FindIFoodElementId(byte kind, long localId) =>
        _elements.FirstOrDefault(e => e.IsActive && e.Kind == kind && e.LocalId == localId)?.IFoodElementId;

    public void UpdateIFoodPizzaId(string ifoodPizzaId)
    {
        IFoodPizzaId = ifoodPizzaId;
        UpdatedAt = DateTime.Now;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
    }
}

// Constantes de "kind" (byte, sem tabela lookup — mesmo espírito de CashMovementType ser
// referenciado por id direto em alguns pontos legados, mas aqui nem isso: é só um discriminador
// interno do SyncBar, nunca exposto/validado pelo iFood).
public static class IFoodPizzaElementKind
{
    public const byte Size = 1;
    public const byte Crust = 2;
    public const byte Edge = 3;
    public const byte Topping = 4;
}
