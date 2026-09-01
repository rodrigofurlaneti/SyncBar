using System.Threading;
using SyncBar.Application.Abstractions.Integrations.Ifood;

namespace SyncBar.Infrastructure.Integrations.Ifood;

/// <summary>
/// Implementação em memória de IIfoodOperationalAlertStore (Fase 13) — ver comentário na
/// interface pra justificativa do trade-off "sem persistência". Registrada como Singleton (não
/// Scoped) porque precisa sobreviver entre os ciclos do BackgroundService, que roda fora de
/// qualquer request HTTP.
/// </summary>
internal sealed class InMemoryIfoodOperationalAlertStore : IIfoodOperationalAlertStore
{
    // Por empresa, guarda só os últimos N alertas não reconhecidos — evita crescer sem limite se
    // ninguém abrir a tela por dias (ex.: loja oscilando disponível/indisponível repetidamente
    // por causa de uma instabilidade de internet na loja).
    private const int MaxPerCompany = 50;

    private readonly Dictionary<long, List<IfoodOperationalAlert>> _byCompany = [];
    private readonly Lock _lock = new();

    public IfoodOperationalAlert Raise(
        long companyId, long branchId, string branchName, string title, string message, IfoodOperationalAlertSeverity severity)
    {
        var alert = new IfoodOperationalAlert(Guid.NewGuid(), companyId, branchId, branchName, title, message, severity, DateTime.Now);

        lock (_lock)
        {
            if (!_byCompany.TryGetValue(companyId, out var list))
            {
                list = [];
                _byCompany[companyId] = list;
            }

            list.Add(alert);
            if (list.Count > MaxPerCompany)
                list.RemoveRange(0, list.Count - MaxPerCompany);
        }

        return alert;
    }

    public IReadOnlyList<IfoodOperationalAlert> GetUnacknowledged(long companyId)
    {
        lock (_lock)
        {
            return _byCompany.TryGetValue(companyId, out var list)
                ? list.OrderByDescending(a => a.CreatedAtUtc).ToList()
                : [];
        }
    }

    public bool Acknowledge(long companyId, Guid alertId)
    {
        lock (_lock)
        {
            if (!_byCompany.TryGetValue(companyId, out var list))
                return false;

            var index = list.FindIndex(a => a.Id == alertId);
            if (index < 0)
                return false;

            list.RemoveAt(index);
            return true;
        }
    }
}
