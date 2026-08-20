using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Cadastro LEVE pra complementos puramente descritivos (ex.: "sem cebola", "bacon extra") que o
// iFood exige apontarem pra um "produto", mas que o SyncBar nunca vende sozinho no balcão — por
// isso é uma entidade própria, separada de Product (que carrega categoria/estoque/preço de
// balcão, irrelevantes aqui). Decisão tomada com o usuário na Fase 6a: ComplementItem leve em
// vez de forçar todo complemento a virar um Product completo.
public sealed class ComplementItem : AggregateRoot
{
    public long CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private ComplementItem() : base(0) { }

    private ComplementItem(long companyId, string name) : base(0)
    {
        CompanyId = companyId;
        Name = name;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<ComplementItem> Create(long companyId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<ComplementItem>(new Error("ComplementItem.EmptyName", "Name is required."));

        return Result.Success(new ComplementItem(companyId, name));
    }

    public Result UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(new Error("ComplementItem.EmptyName", "Name is required."));

        Name = name;
        UpdatedAt = DateTime.Now;
        return Result.Success();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
    }
}
