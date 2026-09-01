namespace SyncBar.Application.Abstractions.Integrations.Ifood;

public sealed record IfoodAuthResult(bool Success, string? AccessToken, int? ExpiresInSeconds, string? ErrorMessage);

/// <summary>
/// Abstração para autenticação OAuth2 (client_credentials) contra a API do Ifood.
/// A implementação real troca a Infrastructure.Integrations.Ifood.IfoodAuthClient registrada
/// por padrão em SyncBar.Infrastructure.DependencyInjection.AddInfrastructure — mesmo padrão
/// usado para IPaymentGatewayService/IFiscalDocumentService.
/// </summary>
public interface IIfoodAuthClient
{
    Task<IfoodAuthResult> AuthenticateAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default);
}
