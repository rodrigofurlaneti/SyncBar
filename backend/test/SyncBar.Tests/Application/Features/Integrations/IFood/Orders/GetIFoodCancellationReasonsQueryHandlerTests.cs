using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Features.Integrations.IFood.Orders;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Orders;

public sealed class GetIFoodCancellationReasonsQueryHandlerTests
{
    private const string IFoodOrderExternalId = "ifood-order-123";
    private const string ValidToken = "valid-token";
    private const long BranchId = 10;
    private const long CompanyId = 1;

    private readonly IIFoodOrderRepository _ifoodOrderRepository = Substitute.For<IIFoodOrderRepository>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIFoodTokenProvider _tokenProvider = Substitute.For<IIFoodTokenProvider>();
    private readonly IIFoodOrderClient _orderClient = Substitute.For<IIFoodOrderClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private GetIFoodCancellationReasonsQueryHandler CreateSut() =>
        new(_ifoodOrderRepository, _branchRepository, _tokenProvider, _orderClient, _logRepository, _unitOfWork);

    private static IFoodOrder CreateIFoodOrder(long branchId = BranchId) =>
        IFoodOrder.Create(
            customerOrderId: 1, branchId: branchId, ifoodOrderId: IFoodOrderExternalId, displayId: "001",
            merchantId: "merchant-1", ifoodOrderType: "DELIVERY", deliveredBy: "IFOOD", orderTiming: "IMMEDIATE",
            preparationStartDateTime: null, now: DateTime.Now, hasUnmappedItems: false).Value;

    private static Branch CreateBranch(long companyId = CompanyId) =>
        Branch.Create(companyId, "Filial Centro", null, null, null, null, null, null, null, null).Value;

    [Fact]
    public async Task Handle_WhenIFoodOrderNotFound_ShouldFail()
    {
        _ifoodOrderRepository.GetByIdForUpdateAsync(99, Arg.Any<CancellationToken>()).Returns((IFoodOrder?)null);
        var sut = CreateSut();

        var result = await sut.Handle(new GetIFoodCancellationReasonsQuery(99), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IFoodOrder.NotFound");
    }

    [Fact]
    public async Task Handle_WhenBranchNotFound_ShouldFail()
    {
        _ifoodOrderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(CreateIFoodOrder());
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);
        var sut = CreateSut();

        var result = await sut.Handle(new GetIFoodCancellationReasonsQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.NotFound");
    }

    // Diferente dos demais handlers deste módulo: sem token, este cai num caminho de sucesso com
    // lista vazia (não falha) — a tela mostra "sem motivos disponíveis" em vez de erro bloqueante.
    [Fact]
    public async Task Handle_WhenIFoodTokenUnavailable_ShouldSucceedWithEmptyList()
    {
        _ifoodOrderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(CreateIFoodOrder());
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns((string?)null);
        var sut = CreateSut();

        var result = await sut.Handle(new GetIFoodCancellationReasonsQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenIFoodReturnsReasons_ShouldMapCodeAndDescription()
    {
        _ifoodOrderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(CreateIFoodOrder());
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(ValidToken);
        _orderClient.GetCancellationReasonsAsync(ValidToken, IFoodOrderExternalId, Arg.Any<CancellationToken>())
            .Returns(new List<IFoodCancellationReasonDto>
            {
                new("501", "Estabelecimento fechado"),
                new("502", "Item fora de estoque"),
            });
        var sut = CreateSut();

        var result = await sut.Handle(new GetIFoodCancellationReasonsQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().ContainSingle(r => r.Code == "501" && r.Description == "Estabelecimento fechado");
        result.Value.Should().ContainSingle(r => r.Code == "502" && r.Description == "Item fora de estoque");
    }
}
