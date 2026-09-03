using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Logistics;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Logistics;

public sealed class GetIfoodLogisticsOrderDetailsQueryHandlerTests
{
    private readonly IIfoodOrderRepository _ifoodOrderRepository = Substitute.For<IIfoodOrderRepository>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodLogisticsClient _logisticsClient = Substitute.For<IIfoodLogisticsClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetIfoodLogisticsOrderDetailsQueryHandler _handler;

    public GetIfoodLogisticsOrderDetailsQueryHandlerTests()
    {
        _handler = new GetIfoodLogisticsOrderDetailsQueryHandler(
            _ifoodOrderRepository, _branchRepository, _tokenProvider, _logisticsClient, _logRepository, _unitOfWork);
    }

    private static IfoodOrder CreateOrder()
        => IfoodOrder.Create(
            customerOrderId: 1, branchId: 1, "ifood-order-1", null, "MERCH-1", "DELIVERY", null, "IMMEDIATE", null,
            now: DateTime.Now, hasUnmappedItems: false).Value;

    private static Branch CreateBranch()
        => Branch.Create(
            companyId: 1, "Loja Centro", cnpj: null, phone: null, addressStreet: null, addressNumber: null,
            addressDistrict: null, addressCity: null, addressState: null, addressZipCode: null).Value;

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnFailure()
    {
        var query = new GetIfoodLogisticsOrderDetailsQuery(IfoodOrderId: 1);
        _ifoodOrderRepository.GetByIdForUpdateAsync(query.IfoodOrderId, Arg.Any<CancellationToken>()).Returns((IfoodOrder?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodOrder.NotFound");
    }

    [Fact]
    public async Task Handle_BranchNotFound_ShouldReturnFailure()
    {
        var order = CreateOrder();
        var query = new GetIfoodLogisticsOrderDetailsQuery(IfoodOrderId: 1);
        _ifoodOrderRepository.GetByIdForUpdateAsync(query.IfoodOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.NotFound");
    }

    [Fact]
    public async Task Handle_TokenUnavailable_ShouldReturnFailure()
    {
        var order = CreateOrder();
        var branch = CreateBranch();
        var query = new GetIfoodLogisticsOrderDetailsQuery(IfoodOrderId: 1);
        _ifoodOrderRepository.GetByIdForUpdateAsync(query.IfoodOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns((string?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ifood.NotConnected");
    }

    [Fact]
    public async Task Handle_IfoodDetailsFail_ShouldReturnFailure()
    {
        var order = CreateOrder();
        var branch = CreateBranch();
        var query = new GetIfoodLogisticsOrderDetailsQuery(IfoodOrderId: 1);
        _ifoodOrderRepository.GetByIdForUpdateAsync(query.IfoodOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _logisticsClient.GetOrderDetailsAsync("token-1", order.IfoodOrderId, Arg.Any<CancellationToken>())
            .Returns(new IfoodLogisticsOrderDetailsResult(false, null, "Erro ao buscar detalhes."));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ifood.LogisticsOrderDetailsFailed");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldReturnRawPayload()
    {
        var order = CreateOrder();
        var branch = CreateBranch();
        var query = new GetIfoodLogisticsOrderDetailsQuery(IfoodOrderId: 1);
        _ifoodOrderRepository.GetByIdForUpdateAsync(query.IfoodOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _logisticsClient.GetOrderDetailsAsync("token-1", order.IfoodOrderId, Arg.Any<CancellationToken>())
            .Returns(new IfoodLogisticsOrderDetailsResult(true, "{\"status\":\"OK\"}", null));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RawPayload.Should().Be("{\"status\":\"OK\"}");
    }
}
