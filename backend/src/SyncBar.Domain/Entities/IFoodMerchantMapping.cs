using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Liga uma filial (loja física) ao "merchant" correspondente dela no Ifood — o MerchantId
// (e o MerchantUuid, usado em algumas chamadas) que aparece na tela "Permissões" do app no
// portal do Ifood. As credenciais (client_id/client_secret) ficam em IfoodIntegrationSetting,
// por empresa — este mapeamento é só o "qual loja é qual merchant", por filial.
public sealed class IfoodMerchantMapping : AggregateRoot
{
    public long BranchId { get; private set; }
    public string? MerchantId { get; private set; }
    public string? MerchantUuid { get; private set; }
    // Tempo de preparo customizado (minutos) — sobrescreve a estimativa automática do Ifood pra
    // essa loja (módulo Merchant, POST/PUT/DELETE /myPreparationTime, ver Fase 5). Nulo = usa a
    // estimativa automática do Ifood (comportamento padrão, nunca configurado ainda).
    public int? PreparationTimeMinutes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private IfoodMerchantMapping() : base(0) { }

    private IfoodMerchantMapping(long branchId) : base(0)
    {
        BranchId = branchId;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<IfoodMerchantMapping> Create(long branchId)
        => Result.Success(new IfoodMerchantMapping(branchId));

    public Result SetMerchant(string? merchantId, string? merchantUuid)
    {
        MerchantId = merchantId;
        MerchantUuid = merchantUuid;
        UpdatedAt = DateTime.Now;
        return Result.Success();
    }

    // Nulo = remove a customização (o handler chama DELETE /myPreparationTime no Ifood antes de
    // gravar nulo aqui — ver SetIfoodPreparationTimeCommandHandler).
    public Result SetPreparationTime(int? minutes)
    {
        if (minutes is < 0)
            return Result.Failure(new Error("IfoodMerchantMapping.InvalidPreparationTime", "Preparation time cannot be negative."));

        PreparationTimeMinutes = minutes;
        UpdatedAt = DateTime.Now;
        return Result.Success();
    }
}
