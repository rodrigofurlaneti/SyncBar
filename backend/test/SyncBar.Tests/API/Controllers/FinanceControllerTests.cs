using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Finance;
using SyncBar.Application.Features.Finance.CreateCost;
using SyncBar.Application.Features.Finance.DeactivateCost;
using SyncBar.Application.Features.Finance.GetCommissionReport;
using SyncBar.Application.Features.Finance.GetSalesReport;
using SyncBar.Application.Features.Finance.GetScenarios;
using SyncBar.Application.Features.Finance.GetSummary;
using SyncBar.Application.Features.Finance.SetTarget;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class FinanceControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FinanceController _controller;

    public FinanceControllerTests()
    {
        _controller = new FinanceController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetSummary_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetBillingSummaryQuery>(q => q.BranchId == 1 && q.ReferenceYear == 2026 && q.ReferenceMonth == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((BillingSummaryResponse)null!));

        var result = await _controller.GetSummary(1, 2026, 1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSummary_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetBillingSummaryQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<BillingSummaryResponse>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetSummary(999, 2026, 1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetSalesReport_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetSalesReportQuery>(q => q.BranchId == 1 && q.ReferenceYear == 2026 && q.ReferenceMonth == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((SalesReportResponse)null!));

        var result = await _controller.GetSalesReport(1, 2026, 1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSalesReport_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetSalesReportQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<SalesReportResponse>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetSalesReport(999, 2026, 1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetScenarios_Success_ShouldForwardOptionalMarginsAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetScenariosQuery>(q =>
                q.BranchId == 1 && q.ReferenceYear == 2026 && q.ReferenceMonth == 1
                && q.DesiredProfit == 1000m && q.PessimisticMargin == 0.1m && q.NormalMargin == null && q.OptimisticMargin == 0.3m),
            Arg.Any<CancellationToken>()).Returns(Result.Success((ScenariosResponse)null!));

        var result = await _controller.GetScenarios(1, 2026, 1, 1000m, 0.1m, null, 0.3m, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetScenarios_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetScenariosQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ScenariosResponse>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetScenarios(999, 2026, 1, 0, null, null, null, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetCommissionReport_Success_ShouldForwardDateRangeAndReturnOk()
    {
        var from = DateTime.Today;
        var to = DateTime.Today.AddDays(30);
        _mediator.Send(Arg.Is<GetCommissionReportQuery>(q => q.BranchId == 1 && q.From == from && q.To == to), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<EmployeeCommissionResponse>>([]));

        var result = await _controller.GetCommissionReport(1, from, to, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCommissionReport_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetCommissionReportQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<EmployeeCommissionResponse>>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetCommissionReport(999, DateTime.Today, DateTime.Today, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateCost_Success_ShouldReturnOkWithValue()
    {
        var command = new CreateOperatingCostCommand(1, 1, "Aluguel", 2000m, 2026, 1);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(3L));

        var result = await _controller.CreateCost(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(3L);
    }

    [Fact]
    public async Task CreateCost_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new CreateOperatingCostCommand(1, 1, "", -1m, 2026, 1);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("OperatingCost.Invalid", "custo invalido")));

        var result = await _controller.CreateCost(command, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeactivateCost_Success_ShouldReturnNoContent()
    {
        _mediator.Send(Arg.Is<DeactivateOperatingCostCommand>(c => c.OperatingCostId == 1), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.DeactivateCost(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeactivateCost_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DeactivateOperatingCostCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("OperatingCost.NotFound", "custo nao encontrado")));

        var result = await _controller.DeactivateCost(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetTarget_Success_ShouldReturnOkWithValue()
    {
        var command = new SetRevenueTargetCommand(1, 2026, 1, 50000m);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(7L));

        var result = await _controller.SetTarget(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(7L);
    }

    [Fact]
    public async Task SetTarget_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new SetRevenueTargetCommand(1, 2026, 1, -1m);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("RevenueTarget.Invalid", "meta invalida")));

        var result = await _controller.SetTarget(command, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
