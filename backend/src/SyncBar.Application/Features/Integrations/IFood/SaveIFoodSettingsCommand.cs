using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood;

// Credenciais são por EMPRESA (o app do Ifood é "centralizado": um client_id/client_secret dá
// acesso a vários merchants — ver IfoodIntegrationSetting.cs). ClientSecret em branco = "manter
// o segredo já salvo" (o frontend nunca reexibe o valor salvo).
// IfoodCustomerId: exigido só pelos endpoints de tempo de preparo do módulo Merchant (Fase 5,
// header X-Ifood-Customer-ID) — opcional, não é segredo, texto puro.
public sealed record SaveIfoodSettingsCommand(
    long CompanyId, string? ClientId, string? ClientSecret, bool Enabled, string? IfoodCustomerId = null) : ICommand;
