using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Liga uma filial (loja física) ao "merchant" correspondente dela no iFood — o MerchantId
// (e o MerchantUuid, usado em algumas chamadas) que aparece na tela "Permissões" do app no
// portal do iFood. As credenciais (client_id/client_secret) ficam em IFoodIntegrationSetting,
// por empresa — este mapeamento é só o "qual loja é qual merchant", por filial.
public sealed class IFoodMerchantMapping : AggregateRoot
{
    public long BranchId { get; private set; }
    public string? MerchantId { get; private set; }
    public string? MerchantUuid { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private IFoodMerchantMapping() : base(0) { }

    private IFoodMerchantMapping(long branchId) : base(0)
    {
        BranchId = branchId;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<IFoodMerchantMapping> Create(long branchId)
        => Result.Success(new IFoodMerchantMapping(branchId));

    public Result SetMerchant(string? merchantId, string? merchantUuid)
    {
        MerchantId = merchantId;
        MerchantUuid = merchantUuid;
        UpdatedAt = DateTime.Now;
        return Result.Success();
    }
}
