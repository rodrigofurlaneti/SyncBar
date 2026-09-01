namespace SyncBar.Application.Features.Integrations.Ifood.Analytics;

// Buckets = JSON bruto de cada grupo (ex.: 1 bucket por canal de venda) — ver nota em
// IIfoodAnalyticsClient sobre por que a resposta não é tipada campo-a-campo.
public sealed record IfoodOrderKpisResponse(int CurrentPage, IReadOnlyCollection<string> Buckets);
