using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Features.Integrations.IFood.Orders;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

// Demonstra o padrao de BDD "no nivel do handler" (Application), diferente de
// CustomerOrderSteps.cs (que exercita a entidade de dominio diretamente): aqui os
// colaboradores (repositorios, cliente HTTP do iFood, token provider) sao dublados com Moq —
// unico mock disponivel neste projeto (SyncBar.Specs.csproj usa Moq; SyncBar.Tests usa
// NSubstitute — inconsistencia pre-existente entre os dois projetos de teste, nao introduzida
// aqui) — para exercitar o CQRS handler real de ponta a ponta como o MediatR faria em runtime.
[Binding]
public sealed class MarkIFoodOrderReadySteps
{
    private const string IFoodOrderExternalId = "ifood-order-1";
    private const string ValidToken = "valid-token";
    private const long CompanyId = 1;

    private readonly Mock<IIFoodOrderRepository> _ifoodOrderRepository = new();
    private readonly Mock<IBranchRepository> _branchRepository = new();
    private readonly Mock<IIFoodTokenProvider> _tokenProvider = new();
    private readonly Mock<IIFoodOrderClient> _orderClient = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private IFoodOrder? _ifoodOrder;
    private Result? _result;

    [Given(@"nao existe nenhum pedido iFood com o id (.*)")]
    public void GivenNaoExisteNenhumPedidoIFoodComOId(long id)
        => _ifoodOrderRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IFoodOrder?)null);

    [Given(@"um pedido iFood aberto com id (.*) na filial (.*)")]
    public void GivenUmPedidoIFoodAbertoComIdNaFilial(long id, long branchId)
    {
        _ifoodOrder = IFoodOrder.Create(
            customerOrderId: 1, branchId: branchId, ifoodOrderId: IFoodOrderExternalId, displayId: "001",
            merchantId: "merchant-1", ifoodOrderType: "DELIVERY", deliveredBy: "IFOOD", orderTiming: "IMMEDIATE",
            preparationStartDateTime: null, now: DateTime.Now, hasUnmappedItems: false).Value;

        _ifoodOrderRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_ifoodOrder);

        _branchRepository
            .Setup(r => r.GetByIdAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Branch.Create(CompanyId, "Filial Centro", null, null, null, null, null, null, null, null).Value);
    }

    [Given(@"a filial (.*) esta conectada ao iFood com um token valido")]
    public void GivenAFilialEstaConectadaAoIFoodComUmTokenValido(long branchId)
        => _tokenProvider
            .Setup(p => p.GetAccessTokenAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidToken);

    [Given(@"a filial (.*) nao tem um token valido do iFood")]
    public void GivenAFilialNaoTemUmTokenValidoDoIFood(long branchId)
        => _tokenProvider
            .Setup(p => p.GetAccessTokenAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

    [Given(@"o iFood aceita a chamada de pronto para retirada")]
    public void GivenOIFoodAceitaAChamadaDeProntoParaRetirada()
        => _orderClient
            .Setup(c => c.ReadyToPickupAsync(ValidToken, IFoodOrderExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IFoodOrderActionResult(true, null));

    [Given(@"o iFood recusa a chamada de pronto para retirada")]
    public void GivenOIFoodRecusaAChamadaDeProntoParaRetirada()
        => _orderClient
            .Setup(c => c.ReadyToPickupAsync(ValidToken, IFoodOrderExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IFoodOrderActionResult(false, "Pedido já concluído no iFood."));

    [When(@"eu tento marcar o pedido iFood (.*) como pronto")]
    public async Task WhenEuTentoMarcarOPedidoIFoodComoPronto(long id)
    {
        var handler = new MarkIFoodOrderReadyCommandHandler(
            _ifoodOrderRepository.Object, _branchRepository.Object, _tokenProvider.Object,
            _orderClient.Object, TimeProvider.System, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new MarkIFoodOrderReadyCommand(id), CancellationToken.None);
    }

    [Then(@"a operacao deve falhar com o erro ""(.*)""")]
    public void ThenAOperacaoDeveFalharComOErro(string errorCode)
    {
        _result!.IsFailure.Should().BeTrue();
        _result.Error.Code.Should().Be(errorCode);
    }

    [Then(@"a operacao deve ter sucesso")]
    public void ThenAOperacaoDeveTerSucesso()
        => _result!.IsSuccess.Should().BeTrue();

    [Then(@"o status do pedido iFood deve ser ""(.*)""")]
    public void ThenOStatusDoPedidoIFoodDeveSer(string status)
        => _ifoodOrder!.Status.Should().Be(status);
}
