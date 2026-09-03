using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Orders;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

// Demonstra o padrao de BDD "no nivel do handler" (Application), diferente de
// CustomerOrderSteps.cs (que exercita a entidade de dominio diretamente): aqui os
// colaboradores (repositorios, cliente HTTP do Ifood, token provider) sao dublados com Moq —
// unico mock disponivel neste projeto (SyncBar.Specs.csproj usa Moq; SyncBar.Tests usa
// NSubstitute — inconsistencia pre-existente entre os dois projetos de teste, nao introduzida
// aqui) — para exercitar o CQRS handler real de ponta a ponta como o MediatR faria em runtime.
[Binding]
[Scope(Feature = "Marcar pedido Ifood como pronto para retirada")]
public sealed class MarkIfoodOrderReadySteps
{
    private const string IfoodOrderExternalId = "Ifood-order-1";
    private const string ValidToken = "valid-token";
    private const long CompanyId = 1;

    private readonly Mock<IIfoodOrderRepository> _IfoodOrderRepository = new();
    private readonly Mock<IBranchRepository> _branchRepository = new();
    private readonly Mock<IIfoodTokenProvider> _tokenProvider = new();
    private readonly Mock<IIfoodOrderClient> _orderClient = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private IfoodOrder? _IfoodOrder;
    private Result? _result;

    [Given(@"nao existe nenhum pedido Ifood com o id (.*)")]
    public void GivenNaoExisteNenhumPedidoIfoodComOId(long id)
        => _IfoodOrderRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IfoodOrder?)null);

    [Given(@"um pedido Ifood aberto com id (.*) na filial (.*)")]
    public void GivenUmPedidoIfoodAbertoComIdNaFilial(long id, long branchId)
    {
        _IfoodOrder = IfoodOrder.Create(
            customerOrderId: 1, branchId: branchId, IfoodOrderId: IfoodOrderExternalId, displayId: "001",
            merchantId: "merchant-1", IfoodOrderType: "DELIVERY", deliveredBy: "Ifood", orderTiming: "IMMEDIATE",
            preparationStartDateTime: null, now: DateTime.Now, hasUnmappedItems: false).Value;

        _IfoodOrderRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_IfoodOrder);

        _branchRepository
            .Setup(r => r.GetByIdAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Branch.Create(CompanyId, "Filial Centro", null, null, null, null, null, null, null, null).Value);
    }

    [Given(@"a filial (.*) esta conectada ao Ifood com um token valido")]
    public void GivenAFilialEstaConectadaAoIfoodComUmTokenValido(long branchId)
        => _tokenProvider
            .Setup(p => p.GetAccessTokenAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidToken);

    [Given(@"a filial (.*) nao tem um token valido do Ifood")]
    public void GivenAFilialNaoTemUmTokenValidoDoIfood(long branchId)
        => _tokenProvider
            .Setup(p => p.GetAccessTokenAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

    [Given(@"o Ifood aceita a chamada de pronto para retirada")]
    public void GivenOIfoodAceitaAChamadaDeProntoParaRetirada()
        => _orderClient
            .Setup(c => c.ReadyToPickupAsync(ValidToken, IfoodOrderExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IfoodOrderActionResult(true, null));

    [Given(@"o Ifood recusa a chamada de pronto para retirada")]
    public void GivenOIfoodRecusaAChamadaDeProntoParaRetirada()
        => _orderClient
            .Setup(c => c.ReadyToPickupAsync(ValidToken, IfoodOrderExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IfoodOrderActionResult(false, "Pedido já concluído no Ifood."));

    [When(@"eu tento marcar o pedido Ifood (.*) como pronto")]
    public async Task WhenEuTentoMarcarOPedidoIfoodComoPronto(long id)
    {
        var handler = new MarkIfoodOrderReadyCommandHandler(
            _IfoodOrderRepository.Object, _branchRepository.Object, _tokenProvider.Object,
            _orderClient.Object, TimeProvider.System, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new MarkIfoodOrderReadyCommand(id), CancellationToken.None);
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

    [Then(@"o status do pedido Ifood deve ser ""(.*)""")]
    public void ThenOStatusDoPedidoIfoodDeveSer(string status)
        => _IfoodOrder!.Status.Should().Be(status);
}
