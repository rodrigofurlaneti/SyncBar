namespace SyncBar.Application.Abstractions.Integrations.Ifood;

public enum IfoodOperationalAlertSeverity
{
    Info,
    Warning,
    Critical
}

public sealed record IfoodOperationalAlert(
    Guid Id,
    long CompanyId,
    long BranchId,
    string BranchName,
    string Title,
    string Message,
    IfoodOperationalAlertSeverity Severity,
    DateTime CreatedAtUtc);

/// <summary>
/// Central simples de alertas operacionais do módulo Ifood (Fase 13). Hoje só é usada pelo
/// IfoodMerchantStatusWatcherBackgroundService pra avisar quando uma loja fica indisponível (ou
/// volta a ficar disponível) no Ifood, mas fica aberta pra qualquer outro job de fundo do módulo
/// Ifood publicar alerta no futuro (ex.: avaliação nova com nota baixa, discrepância financeira
/// achada pelo sync diário) sem precisar inventar mais um mecanismo de notificação.
///
/// Guardado só em memória (singleton, por processo) — mesmo trade-off já aceito em outras partes
/// do módulo Ifood (cache de token em IIfoodTokenProvider, dedup de evento no polling de
/// pedidos): perde o histórico se a API reiniciar, mas evita criar tabela/migração só pra um
/// aviso efêmero que o operador só precisa ver uma vez, na hora em que acontece. Se um dia o
/// SyncBar rodar em mais de uma instância da API ao mesmo tempo, cada instância vai ter sua
/// própria lista de alertas — não é um problema hoje (implantação é de uma instância só, mesma
/// premissa já assumida pelo cache de token).
/// </summary>
public interface IIfoodOperationalAlertStore
{
    IfoodOperationalAlert Raise(
        long companyId, long branchId, string branchName, string title, string message, IfoodOperationalAlertSeverity severity);

    IReadOnlyList<IfoodOperationalAlert> GetUnacknowledged(long companyId);

    bool Acknowledge(long companyId, Guid alertId);
}
