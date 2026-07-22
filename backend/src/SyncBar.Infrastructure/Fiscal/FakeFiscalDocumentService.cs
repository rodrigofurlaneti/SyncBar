using SyncBar.Application.Abstractions.Fiscal;

namespace SyncBar.Infrastructure.Fiscal;

/// <summary>
/// Implementação fake da emissão fiscal (NFC-e) — não fala com nenhuma SEFAZ.
/// Autoriza qualquer documento instantaneamente com uma chave de acesso falsa,
/// apenas para permitir desenvolver o restante do fluxo (impressão, vínculo com a venda)
/// sem certificado digital/credenciais de um provedor real (ex.: Focus NFe, eNotas).
/// </summary>
internal sealed class FakeFiscalDocumentService : IFiscalDocumentService
{
    public Task<FiscalDocumentResult> IssueAsync(FiscalDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var documentId = $"FAKE-NFCE-{Guid.NewGuid():N}";
        return Task.FromResult(new FiscalDocumentResult(
            DocumentId: documentId,
            Status: FiscalDocumentStatus.Authorized,
            AccessKey: string.Concat(Enumerable.Repeat("0", 44)),
            AuthorizationProtocol: $"PROT-{DateTime.UtcNow:yyyyMMddHHmmss}",
            DanfeUrl: null));
    }

    public Task<FiscalDocumentResult> CancelAsync(string documentId, string reason, CancellationToken cancellationToken = default)
        => Task.FromResult(new FiscalDocumentResult(documentId, FiscalDocumentStatus.Cancelled));

    public Task<FiscalDocumentResult> GetStatusAsync(string documentId, CancellationToken cancellationToken = default)
        => Task.FromResult(new FiscalDocumentResult(documentId, FiscalDocumentStatus.Authorized));
}
