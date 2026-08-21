using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood;

// Credenciais são por EMPRESA (o app do iFood é "centralizado": um client_id/client_secret dá
// acesso a vários merchants — ver IFoodIntegrationSetting.cs). ClientSecret em branco = "manter
// o segredo já salvo" (o frontend nunca reexibe o valor salvo).
// IFoodCustomerId: exigido só pelos endpoints de tempo de preparo do módulo Merchant (Fase 5,
// header X-iFood-Customer-ID) — opcional, não é segredo, texto puro.
public sealed record SaveIFoodSettingsCommand(
    long CompanyId, string? ClientId, string? ClientSecret, bool Enabled, string? IFoodCustomerId = null) : ICommand;
