namespace SyncBar.Application.Features.Integrations.Ifood.Financial;

// Items = JSON bruto de cada registro devolvido pelo Ifood — a doc oficial não documenta o
// schema de resposta campo-a-campo pra estes relatórios (só path + query params de filtro), então
// o frontend exibe/baixa o JSON como está em vez de um schema tipado não confirmado.
public sealed record IfoodFinancialReportResponse(
    string ReportType,
    int Count,
    IReadOnlyCollection<string> Items);

public sealed record IfoodReconciliationOnDemandResponse(string RequestId, string RawPayload);

public sealed record IfoodReconciliationOnDemandStatusResponse(bool Found, string? RawPayload);
