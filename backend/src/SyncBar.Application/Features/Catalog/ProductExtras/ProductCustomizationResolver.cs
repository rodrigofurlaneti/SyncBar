using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;

namespace SyncBar.Application.Features.Catalog.ProductExtras;

internal sealed record ResolvedProductCustomizations(ProductOptionalExtra[] OptionalExtras, ProductBoost[] Boosts);

internal static class ProductCustomizationResolver
{
    public static Result<ResolvedProductCustomizations> Resolve(Product product, IReadOnlyCollection<long>? optionalExtraIds, IReadOnlyCollection<long>? selectedBoostIds)
    {
        var optionalIds = optionalExtraIds ?? [];
        var boostIds = selectedBoostIds ?? [];
        if (optionalIds.Distinct().Count() != optionalIds.Count || boostIds.Distinct().Count() != boostIds.Count)
            return Result.Failure<ResolvedProductCustomizations>(new Error("OrderItem.DuplicateSelection", "Selecione cada opcional ou adicional apenas uma vez."));
        var optionalExtras = product.OptionalExtras.Where(x => optionalIds.Contains(x.Id) && x.ProductId == product.Id && x.IsActive)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToArray();
        var boosts = product.Boosts.Where(x => boostIds.Contains(x.Id) && x.ProductId == product.Id && x.IsActive)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToArray();
        if ((optionalIds.Count > 0 && !product.HasOptionalExtras) || optionalExtras.Length != optionalIds.Count)
            return Result.Failure<ResolvedProductCustomizations>(new Error("OrderItem.OptionalExtraUnavailable", "Um opcional selecionado não está disponível para este produto. Reabra a seleção."));
        if ((boostIds.Count > 0 && !product.HasBoosts) || boosts.Length != boostIds.Count)
            return Result.Failure<ResolvedProductCustomizations>(new Error("OrderItem.BoostUnavailable", "Um adicional selecionado não está disponível para este produto. Reabra a seleção."));
        return Result.Success(new ResolvedProductCustomizations(optionalExtras, boosts));
    }
}
