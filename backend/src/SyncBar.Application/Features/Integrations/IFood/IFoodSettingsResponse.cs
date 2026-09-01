namespace SyncBar.Application.Features.Integrations.Ifood;

// ClientId volta em texto puro (não é segredo — é o "identificador público" do app no Ifood,
// precisa ser reeditável no formulário sem apagar sem querer). ClientSecret NUNCA volta aqui.
public sealed record IfoodSettingsResponse(
    bool HasCredentials,
    string? ClientId,
    bool Enabled,
    DateTime? LastConnectionTestAt,
    bool? LastConnectionTestSucceeded,
    string? IfoodCustomerId);
