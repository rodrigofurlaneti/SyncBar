namespace SyncBar.Application.Features.Integrations.IFood.Analytics;

// Buckets = JSON bruto de cada grupo (ex.: 1 bucket por canal de venda) — ver nota em
// IIFoodAnalyticsClient sobre por que a resposta não é tipada campo-a-campo.
public sealed record IFoodOrderKpisResponse(int CurrentPage, IReadOnlyCollection<string> Buckets);
