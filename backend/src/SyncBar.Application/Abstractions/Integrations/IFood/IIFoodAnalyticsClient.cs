namespace SyncBar.Application.Abstractions.Integrations.Ifood;

// Items = JSON bruto de cada bucket agregado devolvido pelo Ifood. O corpo real do POST
// analytics/v1.0/.../orders/kpis suporta um DSL de filtro+agregação enorme (filtros por
// cancelamento/categoria/canal, N métricas × N funções de agregação por métrica, groupBy
// dinâmico) — a doc oficial (Postman) não documenta os valores válidos de "metrics"/"terms"
// campo-a-campo, então o client de hoje manda um payload padrão razoável (GMV + taxas, agrupado
// por canal de venda, no intervalo de datas pedido) e devolve o JSON bruto de cada bucket, em vez
// de inventar um schema tipado não confirmado pros ~20 possíveis campos de métrica.
public sealed record IfoodOrderKpisResultDto(int CurrentPage, IReadOnlyCollection<string> RawBuckets);

/// <summary>
/// Abstração para o módulo Analytics do Ifood (Fase 9) — analytics/v1.0, 1 endpoint (Search
/// order metrics KPIs). Implementação real: Infrastructure.Integrations.Ifood.IfoodAnalyticsClient.
/// </summary>
public interface IIfoodAnalyticsClient
{
    Task<IfoodOrderKpisResultDto> GetOrderKpisAsync(
        string accessToken, string merchantId, DateTime periodStart, DateTime periodEnd, int page, int size,
        CancellationToken cancellationToken = default);
}
