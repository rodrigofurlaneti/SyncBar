using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

// Passo comum a quase todo handler do módulo Merchant (Fase 5): dado um BranchId, resolve a
// empresa, confere se a integração está habilitada com credenciais, pega um access token válido
// e o MerchantId da filial. Centralizado aqui pra não repetir a mesma sequência de 4 chamadas em
// cada handler (status, interrupções, horários, tempo de preparo).
internal static class IFoodMerchantResolution
{
    public static async Task<Result<(long CompanyId, string MerchantId, string Token, string? IFoodCustomerId)>> ResolveAsync(
        long branchId,
        IBranchRepository branchRepository,
        IIFoodTokenProvider tokenProvider,
        IIFoodIntegrationSettingRepository settingRepository,
        IIFoodMerchantMappingRepository mappingRepository,
        CancellationToken cancellationToken)
    {
        var branch = await branchRepository.GetByIdAsync(branchId, cancellationToken);
        if (branch is null)
            return Result.Failure<(long, string, string, string?)>(new Error("IFoodMerchant.BranchNotFound", "Branch not found."));

        var setting = await settingRepository.GetByCompanyAsync(branch.CompanyId, cancellationToken);
        if (setting is null || !setting.Enabled || string.IsNullOrWhiteSpace(setting.ClientId))
            return Result.Failure<(long, string, string, string?)>(new Error("IFoodMerchant.NotConfigured", "iFood integration is not configured or is disabled for this company."));

        var mapping = await mappingRepository.GetByBranchAsync(branchId, cancellationToken);
        if (mapping is null || string.IsNullOrWhiteSpace(mapping.MerchantId))
            return Result.Failure<(long, string, string, string?)>(new Error("IFoodMerchant.NoMerchantId", "This branch has no iFood Merchant ID configured."));

        var token = await tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
        if (token is null)
            return Result.Failure<(long, string, string, string?)>(new Error("IFoodMerchant.NoToken", "Could not obtain a valid iFood access token — check the saved credentials."));

        return Result.Success((branch.CompanyId, mapping.MerchantId!, token, setting.IFoodCustomerId));
    }
}
