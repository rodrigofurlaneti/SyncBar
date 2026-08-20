namespace SyncBar.Application.Features.Integrations.IFood.Financial;

// Items = JSON bruto de cada registro devolvido pelo iFood — a doc oficial não documenta o
// schema de resposta campo-a-campo pra estes relatórios (só path + query params de filtro), então
// o frontend exibe/baixa o JSON como está em vez de um schema tipado não confirmado.
public sealed record IFoodFinancialReportResponse(
    string ReportType,
    int Count,
    IReadOnlyCollection<string> Items);

public sealed record IFoodReconciliationOnDemandResponse(string RequestId, string RawPayload);

public sealed record IFoodReconciliationOnDemandStatusResponse(bool Found, string? RawPayload);
