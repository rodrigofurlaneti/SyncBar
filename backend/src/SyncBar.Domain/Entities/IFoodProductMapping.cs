using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Liga um Product do SyncBar ao item correspondente no catálogo do iFood, por FILIAL — assim
// como IFoodCategoryMapping, o catálogo do iFood é por merchant, então cada filial tem o "seu"
// item mesmo sendo o mesmo Product. IFoodItemId/IFoodProductId são GUIDs gerados por nós (o
// iFood exige UUID v4 no campo `id` de item e produto — Guid.NewGuid() já gera nesse formato) e
// persistidos aqui pra todo PUT /items seguinte ser idempotente (reenvia os mesmos ids, nunca
// cria item duplicado). IFoodProductId é o id do objeto "products[0]" dentro do payload do item —
// diferente do IFoodItemId (id do "item" em si), conforme a hierarquia da Catalog API.
public sealed class IFoodProductMapping : AggregateRoot
{
    public long ProductId { get; private set; }
    public long BranchId { get; private set; }
    public Guid IFoodItemId { get; private set; }
    public Guid IFoodProductId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private IFoodProductMapping() : base(0) { }

    private IFoodProductMapping(long productId, long branchId, Guid ifoodItemId, Guid ifoodProductId) : base(0)
    {
        ProductId = productId;
        BranchId = branchId;
        IFoodItemId = ifoodItemId;
        IFoodProductId = ifoodProductId;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<IFoodProductMapping> Create(long productId, long branchId)
        => Result.Success(new IFoodProductMapping(productId, branchId, Guid.NewGuid(), Guid.NewGuid()));
}
