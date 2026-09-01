using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Fase 17 — liga uma PizzaConfiguration do SyncBar à pizza correspondente no catálogo v1 (legado)
// do Ifood, por FILIAL — mesmo padrão de IfoodProductMapping/IfoodCategoryMapping (o catálogo do
// Ifood é por merchant). IfoodPizzaId é o id retornado pelo Ifood na criação (POST
// merchants/{merchantId}/pizzas) — diferente dos outros mapeamentos, aqui o id não é gerado por
// nós porque a API de pizza do v1 não aceita um id proposto no create, só devolve um. Os ids de
// cada size/crust/edge/topping dentro dessa pizza (também devolvidos pelo Ifood na criação, um
// por elemento) ficam nos IfoodPizzaElementMapping filhos — necessários pra todo PUT/PATCH
// seguinte referenciar os elementos certos (idempotência).
public sealed class IfoodPizzaMapping : AggregateRoot
{
    private readonly List<IfoodPizzaElementMapping> _elements = [];

    public long PizzaConfigurationId { get; private set; }
    public long BranchId { get; private set; }
    public string IfoodPizzaId { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }
    public IReadOnlyCollection<IfoodPizzaElementMapping> Elements => _elements.AsReadOnly();

    private IfoodPizzaMapping() : base(0) { }

    private IfoodPizzaMapping(long pizzaConfigurationId, long branchId, string ifoodPizzaId) : base(0)
    {
        PizzaConfigurationId = pizzaConfigurationId;
        BranchId = branchId;
        IfoodPizzaId = ifoodPizzaId;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<IfoodPizzaMapping> Create(long pizzaConfigurationId, long branchId, string IfoodPizzaId)
    {
        if (string.IsNullOrWhiteSpace(IfoodPizzaId))
            return Result.Failure<IfoodPizzaMapping>(new Error("IfoodPizzaMapping.EmptyId", "Ifood pizza id is required."));

        return Result.Success(new IfoodPizzaMapping(pizzaConfigurationId, branchId, IfoodPizzaId));
    }

    // Upsert: se já existe um mapeamento ativo pra esse (kind, localId), atualiza o id do Ifood;
    // senão, cria. `kind` usa as constantes em IfoodPizzaElementKind (Size/Crust/Edge/Topping).
    public IfoodPizzaElementMapping SetElement(byte kind, long localId, string IfoodElementId)
    {
        var existing = _elements.FirstOrDefault(e => e.IsActive && e.Kind == kind && e.LocalId == localId);
        if (existing is not null)
        {
            existing.UpdateIfoodElementId(IfoodElementId);
            UpdatedAt = DateTime.Now;
            return existing;
        }

        var created = IfoodPizzaElementMapping.Create(Id, kind, localId, IfoodElementId);
        _elements.Add(created);
        UpdatedAt = DateTime.Now;
        return created;
    }

    public string? FindIfoodElementId(byte kind, long localId) =>
        _elements.FirstOrDefault(e => e.IsActive && e.Kind == kind && e.LocalId == localId)?.IfoodElementId;

    public void UpdateIfoodPizzaId(string ifoodPizzaId)
    {
        IfoodPizzaId = ifoodPizzaId;
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
// interno do SyncBar, nunca exposto/validado pelo Ifood).
public static class IfoodPizzaElementKind
{
    public const byte Size = 1;
    public const byte Crust = 2;
    public const byte Edge = 3;
    public const byte Topping = 4;
}
