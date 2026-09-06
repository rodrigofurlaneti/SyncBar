using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Fiscal.IssueDocument;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class FiscalControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FiscalController _controller;

    public FiscalControllerTests()
    {
        _controller = new FiscalController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    private static IssueFiscalDocumentCommand ValidCommand() => new(
        SaleId: 1, BranchId: 1, TotalAmount: 50m, CustomerDocument: null,
        Items: [new FiscalDocumentItemInput("Produto", 1, 50m, null)]);

    [Fact]
    public async Task Issue_Success_ShouldReturnOkWithValue()
    {
        var command = ValidCommand();
        var response = new IssueFiscalDocumentResponse("doc-1", "AUTHORIZED", "key-1", "protocol-1");
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(response));

        var result = await _controller.Issue(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task Issue_Failure_ShouldReturnMappedErrorResult()
    {
        var command = ValidCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<IssueFiscalDocumentResponse>(new Error("Sale.NotFound", "venda nao encontrada")));

        var result = await _controller.Issue(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
