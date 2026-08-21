namespace SyncBar.Application.Abstractions.Integrations.IFood;

public sealed record IFoodAuthResult(bool Success, string? AccessToken, int? ExpiresInSeconds, string? ErrorMessage);

/// <summary>
/// Abstração para autenticação OAuth2 (client_credentials) contra a API do iFood.
/// A implementação real troca a Infrastructure.Integrations.IFood.IFoodAuthClient registrada
/// por padrão em SyncBar.Infrastructure.DependencyInjection.AddInfrastructure — mesmo padrão
/// usado para IPaymentGatewayService/IFiscalDocumentService.
/// </summary>
public interface IIFoodAuthClient
{
    Task<IFoodAuthResult> AuthenticateAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default);
}
