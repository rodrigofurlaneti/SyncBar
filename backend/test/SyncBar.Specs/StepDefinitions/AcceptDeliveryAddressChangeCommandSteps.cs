using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Shipping;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Aceitar troca de endereco de entrega no Ifood")]
public sealed class AcceptDeliveryAddressChangeCommandSteps
{
    private const string IfoodOrderExternalId = "ifood-order-1";
    private const string ValidToken = "valid-token";
    private const long CompanyId = 1;

    private readonly Mock<IIfoodOrderRepository> _orderRepository = new();
    private readonly Mock<IBranchRepository> _branchRepository = new();
    private readonly Mock<IIfoodTokenProvider> _tokenProvider = new();
    private readonly Mock<IIfoodShippingClient> _shippingClient = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private IfoodOrder? _order;
    private Result? _result;

    [Given(@"nao existe nenhum pedido Ifood com o id (.*)")]
    public void GivenNaoExisteNenhumPedidoIfoodComOId(long id)
        => _orderRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IfoodOrder?)null);

    [Given(@"um pedido Ifood aberto com id (.*) na filial (.*)")]
    public void GivenUmPedidoIfoodAbertoComIdNaFilial(long id, long branchId)
    {
        _order = IfoodOrder.Create(
            customerOrderId: 1, branchId: branchId, IfoodOrderId: IfoodOrderExternalId, displayId: "001",
            merchantId: "merchant-1", IfoodOrderType: "DELIVERY", deliveredBy: "Ifood", orderTiming: "IMMEDIATE",
            preparationStartDateTime: null, now: DateTime.Now, hasUnmappedItems: false).Value;

        _orderRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_order);

        _branchRepository
            .Setup(r => r.GetByIdAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Branch.Create(CompanyId, "Filial Centro", null, null, null, null, null, null, null, null).Value);
    }

    [Given(@"a filial (.*) nao esta cadastrada")]
    public void GivenAFilialNaoEstaCadastrada(long branchId)
        => _branchRepository
            .Setup(r => r.GetByIdAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Branch?)null);

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

    [Given(@"o Ifood aceita a troca de endereco de entrega")]
    public void GivenOIfoodAceitaATrocaDeEnderecoDeEntrega()
        => _shippingClient
            .Setup(c => c.AcceptDeliveryAddressChangeAsync(ValidToken, IfoodOrderExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IfoodShippingActionResult(true, null));

    [Given(@"o Ifood recusa a troca de endereco de entrega")]
    public void GivenOIfoodRecusaATrocaDeEnderecoDeEntrega()
        => _shippingClient
            .Setup(c => c.AcceptDeliveryAddressChangeAsync(ValidToken, IfoodOrderExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IfoodShippingActionResult(false, "erro remoto"));

    [When(@"eu tento aceitar a troca de endereco do pedido Ifood (.*)")]
    public async Task WhenEuTentoAceitarATrocaDeEnderecoDoPedidoIfood(long id)
    {
        var handler = new AcceptDeliveryAddressChangeCommandHandler(
            _orderRepository.Object, _branchRepository.Object, _tokenProvider.Object, _shippingClient.Object,
            _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new AcceptDeliveryAddressChangeCommand(id), CancellationToken.None);
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
}
