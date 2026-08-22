namespace SyncBar.Application.Abstractions.Integrations.IFood;

public enum IFoodOperationalAlertSeverity
{
    Info,
    Warning,
    Critical
}

public sealed record IFoodOperationalAlert(
    Guid Id,
    long CompanyId,
    long BranchId,
    string BranchName,
    string Title,
    string Message,
    IFoodOperationalAlertSeverity Severity,
    DateTime CreatedAtUtc);

/// <summary>
/// Central simples de alertas operacionais do módulo iFood (Fase 13). Hoje só é usada pelo
/// IFoodMerchantStatusWatcherBackgroundService pra avisar quando uma loja fica indisponível (ou
/// volta a ficar disponível) no iFood, mas fica aberta pra qualquer outro job de fundo do módulo
/// iFood publicar alerta no futuro (ex.: avaliação nova com nota baixa, discrepância financeira
/// achada pelo sync diário) sem precisar inventar mais um mecanismo de notificação.
///
/// Guardado só em memória (singleton, por processo) — mesmo trade-off já aceito em outras partes
/// do módulo iFood (cache de token em IIFoodTokenProvider, dedup de evento no polling de
/// pedidos): perde o histórico se a API reiniciar, mas evita criar tabela/migração só pra um
/// aviso efêmero que o operador só precisa ver uma vez, na hora em que acontece. Se um dia o
/// SyncBar rodar em mais de uma instância da API ao mesmo tempo, cada instância vai ter sua
/// própria lista de alertas — não é um problema hoje (implantação é de uma instância só, mesma
/// premissa já assumida pelo cache de token).
/// </summary>
public interface IIFoodOperationalAlertStore
{
    IFoodOperationalAlert Raise(
        long companyId, long branchId, string branchName, string title, string message, IFoodOperationalAlertSeverity severity);

    IReadOnlyList<IFoodOperationalAlert> GetUnacknowledged(long companyId);

    bool Acknowledge(long companyId, Guid alertId);
}
