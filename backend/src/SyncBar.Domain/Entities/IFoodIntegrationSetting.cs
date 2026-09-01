using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Credenciais da integração com o Ifood — por EMPRESA (CompanyId), não por filial.
//
// Correção feita em 2026-08-19: o app criado no portal do Ifood pelo usuário é do tipo
// "aplicativo centralizado" — UM client_id/client_secret autoriza acesso a VÁRIOS merchants
// (a tela de "Permissões" do app lista os merchants autorizados). Isso significa que as
// credenciais do app são por empresa, não por loja física — cada loja (filial) só precisa do
// seu MerchantId próprio, guardado em IfoodMerchantMapping (BranchId-scoped). Modelo anterior
// (ClientId/ClientSecret por BranchId) exigiria colar o mesmo segredo em toda filial — errado
// e repetitivo para empresas com mais de uma loja.
//
// ClientSecret NUNCA fica em texto puro aqui — chega já criptografado (Data Protection), a
// Application faz a cifra/decifra na borda (handlers), o Domain só guarda o valor opaco.
public sealed class IfoodIntegrationSetting : AggregateRoot
{
    public long CompanyId { get; private set; }
    public string? ClientId { get; private set; }
    public string? ClientSecretEncrypted { get; private set; }
    public bool Enabled { get; private set; }
    // ID de cliente/usuário do desenvolvedor no Ifood — exigido só pelos endpoints de tempo de
    // preparo do módulo Merchant (header X-Ifood-Customer-ID, ver Fase 5 no doc de status). Não
    // é segredo (não é criptografado), fica salvo em texto puro junto do ClientId. Opcional: sem
    // ele, os botões de tempo de preparo ficam desabilitados na tela, o resto da integração
    // funciona normalmente.
    public string? IfoodCustomerId { get; private set; }
    public DateTime? LastConnectionTestAt { get; private set; }
    public bool? LastConnectionTestSucceeded { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private IfoodIntegrationSetting() : base(0) { }

    private IfoodIntegrationSetting(long companyId) : base(0)
    {
        CompanyId = companyId;
        Enabled = false;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<IfoodIntegrationSetting> Create(long companyId)
        => Result.Success(new IfoodIntegrationSetting(companyId));

    // clientSecretEncrypted em branco/nulo = "manter o segredo já salvo" — o frontend nunca
    // reexibe o valor salvo, então reenviar em branco não pode apagar o que já está lá.
    public Result SaveCredentials(string? clientId, string? clientSecretEncrypted, bool enabled, string? ifoodCustomerId)
    {
        ClientId = clientId;
        if (!string.IsNullOrWhiteSpace(clientSecretEncrypted))
            ClientSecretEncrypted = clientSecretEncrypted;
        Enabled = enabled;
        IfoodCustomerId = ifoodCustomerId;
        UpdatedAt = DateTime.Now;
        return Result.Success();
    }

    public void RegisterConnectionTest(bool succeeded)
    {
        LastConnectionTestAt = DateTime.Now;
        LastConnectionTestSucceeded = succeeded;
        UpdatedAt = DateTime.Now;
    }
}
