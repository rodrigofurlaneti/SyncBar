namespace SyncBar.Application.Abstractions.Fiscal;

public enum FiscalDocumentStatus
{
    Pending,
    Authorized,
    Rejected,
    Cancelled
}

public sealed record FiscalDocumentItem(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    string? NcmCode = null);

public sealed record FiscalDocumentRequest(
    long SaleId,
    long BranchId,
    IReadOnlyCollection<FiscalDocumentItem> Items,
    decimal TotalAmount,
    string? CustomerDocument = null);

public sealed record FiscalDocumentResult(
    string DocumentId,
    FiscalDocumentStatus Status,
    string? AccessKey = null,
    string? AuthorizationProtocol = null,
    string? RejectionReason = null,
    string? DanfeUrl = null);

/// <summary>
/// Abstração para emissão de documento fiscal (NFC-e) via um provedor externo
/// (ex.: Focus NFe, eNotas, SEFAZ direto). A implementação real precisa de certificado
/// digital A1 da empresa e credenciais do provedor — a
/// <see cref="Infrastructure.Fiscal.FakeFiscalDocumentService"/> registrada por padrão
/// simula autorização instantânea para permitir desenvolver o restante do fluxo sem essas credenciais.
/// </summary>
public interface IFiscalDocumentService
{
    Task<FiscalDocumentResult> IssueAsync(FiscalDocumentRequest request, CancellationToken cancellationToken = default);

    Task<FiscalDocumentResult> CancelAsync(string documentId, string reason, CancellationToken cancellationToken = default);

    Task<FiscalDocumentResult> GetStatusAsync(string documentId, CancellationToken cancellationToken = default);
}
