using SyncBar.Application.Abstractions.Fiscal;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;

namespace SyncBar.Application.Features.Fiscal.IssueDocument;

internal sealed class IssueFiscalDocumentCommandHandler(IFiscalDocumentService fiscalDocumentService)
    : ICommandHandler<IssueFiscalDocumentCommand, IssueFiscalDocumentResponse>
{
    public async Task<Result<IssueFiscalDocumentResponse>> Handle(IssueFiscalDocumentCommand request, CancellationToken cancellationToken)
    {
        var items = request.Items
            .Select(i => new FiscalDocumentItem(i.Description, i.Quantity, i.UnitPrice, i.NcmCode))
            .ToList();

        var result = await fiscalDocumentService.IssueAsync(
            new FiscalDocumentRequest(request.SaleId, request.BranchId, items, request.TotalAmount, request.CustomerDocument),
            cancellationToken);

        if (result.Status == FiscalDocumentStatus.Rejected)
            return Result.Failure<IssueFiscalDocumentResponse>(
                new Error("FiscalDocument.Rejected", result.RejectionReason ?? "Document rejected by fiscal provider."));

        return Result.Success(new IssueFiscalDocumentResponse(
            result.DocumentId, result.Status.ToString(), result.AccessKey, result.AuthorizationProtocol));
    }
}
