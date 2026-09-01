using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Liga um Product do SyncBar ao item correspondente no catálogo do Ifood, por FILIAL — assim
// como IfoodCategoryMapping, o catálogo do Ifood é por merchant, então cada filial tem o "seu"
// item mesmo sendo o mesmo Product. IfoodItemId/IfoodProductId são GUIDs gerados por nós (o
// Ifood exige UUID v4 no campo `id` de item e produto — Guid.NewGuid() já gera nesse formato) e
// persistidos aqui pra todo PUT /items seguinte ser idempotente (reenvia os mesmos ids, nunca
// cria item duplicado). IfoodProductId é o id do objeto "products[0]" dentro do payload do item —
// diferente do IfoodItemId (id do "item" em si), conforme a hierarquia da Catalog API.
public sealed class IfoodProductMapping : AggregateRoot
{
    public long ProductId { get; private set; }
    public long BranchId { get; private set; }
    public Guid IfoodItemId { get; private set; }
    public Guid IfoodProductId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; }
    public bool IsActive { get; private set; }

    private IfoodProductMapping() : base(0) { }

    private IfoodProductMapping(long productId, long branchId, Guid ifoodItemId, Guid ifoodProductId) : base(0)
    {
        ProductId = productId;
        BranchId = branchId;
        IfoodItemId = ifoodItemId;
        IfoodProductId = ifoodProductId;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<IfoodProductMapping> Create(long productId, long branchId)
        => Result.Success(new IfoodProductMapping(productId, branchId, Guid.NewGuid(), Guid.NewGuid()));
}
