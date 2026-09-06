using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.Authorization
{
    /// <summary>
    /// Garante um access_token Keeta válido para uma empresa/filial: reaproveita o token já
    /// cadastrado em KeetaIntegrationSetting enquanto não estiver perto de expirar; renova via
    /// client_credentials (grant_type=app_level_token) e persiste o novo token quando necessário.
    /// Compartilhado entre o callback de OAuth e a atualização manual de token — e, nas próximas
    /// fases, pelas ações de merchant/pedido que exigem um Bearer token válido.
    /// </summary>
    public interface IKeetaAccessTokenProvider
    {
        Task<Result<string>> GetValidAccessTokenAsync(long companyId, long branchId, bool forceRefresh = false, CancellationToken cancellationToken = default);
    }

    internal sealed class KeetaAccessTokenProvider(
        IKeetaIntegrationSettingRepository settingRepository,
        IKeetaAuthClient authClient,
        IUnitOfWork unitOfWork) : IKeetaAccessTokenProvider
    {
        private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(2);

        public async Task<Result<string>> GetValidAccessTokenAsync(long companyId, long branchId, bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            var setting = await settingRepository.GetByBranchOrCompanyFallbackAsync(companyId, branchId, cancellationToken);

            if (setting is null)
            {
                return Result.Failure<string>(
                    Error.Validation("Keeta.SettingNotConfigured", "Nenhuma configuração do Keeta cadastrada para esta empresa/filial."));
            }

            var hasValidToken = !forceRefresh
                && !string.IsNullOrWhiteSpace(setting.CurrentAccessToken)
                && setting.TokenExpiresAtUtc.HasValue
                && setting.TokenExpiresAtUtc.Value > DateTime.UtcNow.Add(RefreshMargin);

            if (hasValidToken)
                return Result.Success(setting.CurrentAccessToken!);

            var token = await authClient.GetAccessTokenAsync(companyId, branchId, cancellationToken);

            setting.UpdateToken(token.AccessToken, DateTime.UtcNow.AddSeconds(token.ExpiresIn));
            settingRepository.Update(setting);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result.Success(token.AccessToken);
        }
    }
}
