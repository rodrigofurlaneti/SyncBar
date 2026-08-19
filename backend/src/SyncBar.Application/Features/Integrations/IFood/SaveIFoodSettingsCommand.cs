using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood;

// Credenciais são por EMPRESA (o app do iFood é "centralizado": um client_id/client_secret dá
// acesso a vários merchants — ver IFoodIntegrationSetting.cs). ClientSecret em branco = "manter
// o segredo já salvo" (o frontend nunca reexibe o valor salvo).
public sealed record SaveIFoodSettingsCommand(
    long CompanyId, string? ClientId, string? ClientSecret, bool Enabled) : ICommand;
