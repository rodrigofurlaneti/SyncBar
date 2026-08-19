namespace SyncBar.Application.Features.Integrations.IFood;

// ClientId volta em texto puro (não é segredo — é o "identificador público" do app no iFood,
// precisa ser reeditável no formulário sem apagar sem querer). ClientSecret NUNCA volta aqui.
public sealed record IFoodSettingsResponse(
    bool HasCredentials,
    string? ClientId,
    bool Enabled,
    DateTime? LastConnectionTestAt,
    bool? LastConnectionTestSucceeded);
