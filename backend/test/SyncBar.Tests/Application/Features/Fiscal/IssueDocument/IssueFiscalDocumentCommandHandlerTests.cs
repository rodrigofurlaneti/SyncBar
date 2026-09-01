using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Fiscal;
using SyncBar.Application.Features.Fiscal.IssueDocument;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Fiscal.IssueDocument;

public sealed class IssueFiscalDocumentCommandHandlerTests
{
    // Handler não usa repositório de persistência — só o serviço externo de emissão fiscal.
    private readonly IFiscalDocumentService _fiscalDocumentService = Substitute.For<IFiscalDocumentService>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly IssueFiscalDocumentCommandHandler _handler;

    public IssueFiscalDocumentCommandHandlerTests()
    {
        _handler = new IssueFiscalDocumentCommandHandler(_fiscalDocumentService, _logRepository, _unitOfWork);
    }

    private static IssueFiscalDocumentCommand BuildCommand(IReadOnlyCollection<FiscalDocumentItemInput>? items = null)
        => new(
            SaleId: 10,
            BranchId: 1,
            TotalAmount: 150m,
            CustomerDocument: "12345678900",
            Items: items ?? new[]
            {
                new FiscalDocumentItemInput("Chopp 500ml", 3m, 15m, "22030000"),
                new FiscalDocumentItemInput("Porção de fritas", 1m, 45m, "20052000"),
            });

    [Fact]
    public async Task Handle_ServiceAuthorizesDocument_MapsResponseFieldsFromServiceResult()
    {
        var command = BuildCommand();
        var serviceResult = new FiscalDocumentResult(
            DocumentId: "DOC-1",
            Status: FiscalDocumentStatus.Authorized,
            AccessKey: "35260812345678000199650010000001231234567890",
            AuthorizationProtocol: "135260000000123");
        _fiscalDocumentService.IssueAsync(Arg.Any<FiscalDocumentRequest>(), Arg.Any<CancellationToken>())
            .Returns(serviceResult);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DocumentId.Should().Be("DOC-1");
        result.Value.Status.Should().Be(nameof(FiscalDocumentStatus.Authorized));
        result.Value.AccessKey.Should().Be(serviceResult.AccessKey);
        result.Value.AuthorizationProtocol.Should().Be(serviceResult.AuthorizationProtocol);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ServiceRejectsDocument_ReturnsFailureWithRejectionReasonAsErrorMessage()
    {
        var command = BuildCommand();
        var serviceResult = new FiscalDocumentResult(
            DocumentId: "DOC-2",
            Status: FiscalDocumentStatus.Rejected,
            RejectionReason: "CNPJ do emitente inválido");
        _fiscalDocumentService.IssueAsync(Arg.Any<FiscalDocumentRequest>(), Arg.Any<CancellationToken>())
            .Returns(serviceResult);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FiscalDocument.Rejected");
        result.Error.Message.Should().Be("CNPJ do emitente inválido");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ServiceRejectsDocumentWithoutReason_UsesDefaultRejectionMessage()
    {
        var command = BuildCommand();
        var serviceResult = new FiscalDocumentResult(DocumentId: "DOC-3", Status: FiscalDocumentStatus.Rejected);
        _fiscalDocumentService.IssueAsync(Arg.Any<FiscalDocumentRequest>(), Arg.Any<CancellationToken>())
            .Returns(serviceResult);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FiscalDocument.Rejected");
        result.Error.Message.Should().Be("Document rejected by fiscal provider.");
    }

    [Theory]
    [InlineData(FiscalDocumentStatus.Pending)]
    [InlineData(FiscalDocumentStatus.Cancelled)]
    public async Task Handle_ServiceReturnsNonRejectedStatus_ReturnsSuccessWithStatusMappedAsString(FiscalDocumentStatus status)
    {
        var command = BuildCommand();
        var serviceResult = new FiscalDocumentResult(DocumentId: "DOC-4", Status: status);
        _fiscalDocumentService.IssueAsync(Arg.Any<FiscalDocumentRequest>(), Arg.Any<CancellationToken>())
            .Returns(serviceResult);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(status.ToString());
    }

    [Fact]
    public async Task Handle_Always_MapsRequestItemsAndHeaderFieldsToServiceRequest()
    {
        var items = new List<FiscalDocumentItemInput>
        {
            new("Chopp 500ml", 3m, 15m, "22030000"),
            new("Porção de fritas", 1m, 45m, null),
        };
        var command = BuildCommand(items);
        var serviceResult = new FiscalDocumentResult(DocumentId: "DOC-5", Status: FiscalDocumentStatus.Authorized);

        FiscalDocumentRequest? capturedRequest = null;
        _fiscalDocumentService.IssueAsync(Arg.Do<FiscalDocumentRequest>(r => capturedRequest = r), Arg.Any<CancellationToken>())
            .Returns(serviceResult);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.SaleId.Should().Be(command.SaleId);
        capturedRequest.BranchId.Should().Be(command.BranchId);
        capturedRequest.TotalAmount.Should().Be(command.TotalAmount);
        capturedRequest.CustomerDocument.Should().Be(command.CustomerDocument);
        capturedRequest.Items.Should().HaveCount(2);
        capturedRequest.Items.Should().ContainSingle(i =>
            i.Description == "Chopp 500ml" && i.Quantity == 3m && i.UnitPrice == 15m && i.NcmCode == "22030000");
        capturedRequest.Items.Should().ContainSingle(i =>
            i.Description == "Porção de fritas" && i.Quantity == 1m && i.UnitPrice == 45m && i.NcmCode == null);
    }
}
