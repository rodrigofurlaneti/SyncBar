namespace SyncBar.Application.Abstractions.Integrations.IFood;

/// <summary>
/// Cache de access token OAuth2 por empresa — pedido novo a cada chamada não dá (o polling roda
/// a cada 30s), então guarda em memória até faltar pouco pra expirar (usa sempre o `expiresIn`
/// real da resposta do iFood, nunca um tempo fixo — ver IFoodAuthClient). Retorna null se a
/// integração está desabilitada, sem credenciais, ou a autenticação falhou.
/// </summary>
public interface IIFoodTokenProvider
{
    Task<string?> GetAccessTokenAsync(long companyId, CancellationToken cancellationToken = default);
    void Invalidate(long companyId);
}
