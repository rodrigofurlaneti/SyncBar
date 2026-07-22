using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Fiscal.IssueDocument;

public sealed record FiscalDocumentItemInput(string Description, decimal Quantity, decimal UnitPrice, string? NcmCode);

public sealed record IssueFiscalDocumentCommand(
    long SaleId,
    long BranchId,
    decimal TotalAmount,
    string? CustomerDocument,
    IReadOnlyCollection<FiscalDocumentItemInput> Items) : ICommand<IssueFiscalDocumentResponse>;

public sealed record IssueFiscalDocumentResponse(
    string DocumentId,
    string Status,
    string? AccessKey,
    string? AuthorizationProtocol);
