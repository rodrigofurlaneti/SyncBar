using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Cadastro LEVE pra complementos puramente descritivos (ex.: "sem cebola", "bacon extra") que o
// iFood exige apontarem pra um "produto", mas que o SyncBar nunca vende sozinho no balcão — por
// isso é uma entidade própria, separada de Product (que carrega categoria/estoque/preço de
// balcão, irrelevantes aqui). Decisão tomada com o usuário na Fase 6a: ComplementItem leve em
// vez de forçar todo complemento a virar um Product completo.
//
// Fase 18 (combos) — LinkedProductId é a extensão mínima que faltava pra "combo" funcionar sem
// nenhuma entidade nova: quando uma opção de combo precisa ser um item real do cardápio (ex.: o
// grupo "Escolha o sanduíche" dentro do combo precisa mostrar X-Salada com a MESMA imagem/estoque
// do produto avulso), este ComplementItem aponta pro Product em vez de ser só um texto solto.
// Continua sendo o MESMO ComplementItem/Complement/ComplementGroup já usado desde a Fase 6a — só
// ganhou um campo opcional. Nenhuma mudança na sincronização com o iFood (optionGroup/option
// continuam mapeados do mesmo jeito, ver IFoodComplementMapping) além de, quando presente, usar a
// imagem/descrição do produto vinculado (ver MenuComplementsBuilder e SyncIFoodCatalogCommandHandler).
public sealed class ComplementItem : AggregateRoot
{
    public long CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public long? LinkedProductId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private ComplementItem() : base(0) { }

    private ComplementItem(long companyId, string name, long? linkedProductId) : base(0)
    {
        CompanyId = companyId;
        Name = name;
        LinkedProductId = linkedProductId;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<ComplementItem> Create(long companyId, string name, long? linkedProductId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<ComplementItem>(new Error("ComplementItem.EmptyName", "Name is required."));

        return Result.Success(new ComplementItem(companyId, name, linkedProductId));
    }

    public Result UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(new Error("ComplementItem.EmptyName", "Name is required."));

        Name = name;
        UpdatedAt = DateTime.Now;
        return Result.Success();
    }

    // Fase 18 — vincula (ou desvincula, passando null) este item a um Product real do cardápio.
    // Não valida aqui se o Product existe/pertence à mesma empresa — responsabilidade do handler
    // (mesma divisão já usada em LinkProductComplementGroupCommandHandler).
    public void LinkToProduct(long? productId)
    {
        LinkedProductId = productId;
        UpdatedAt = DateTime.Now;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
    }
}
