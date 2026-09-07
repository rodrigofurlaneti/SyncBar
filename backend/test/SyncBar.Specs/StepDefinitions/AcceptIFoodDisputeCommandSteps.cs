using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Orders;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Aceitar disputa do Ifood")]
public sealed class AcceptIFoodDisputeCommandSteps
{
    private const string ValidToken = "valid-token";
    private const long CompanyId = 1;

    private readonly Mock<IBranchRepository> _branchRepository = new();
    private readonly Mock<IIfoodTokenProvider> _tokenProvider = new();
    private readonly Mock<IIfoodOrderClient> _orderClient = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result<IfoodDisputeActionResponse>? _result;

    [Given(@"nao existe nenhuma filial cadastrada com o id (.*)")]
    public void GivenNaoExisteNenhumaFilialCadastradaComOId(long branchId)
        => _branchRepository
            .Setup(r => r.GetByIdAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Branch?)null);

    [Given(@"a filial (.*) esta cadastrada para a empresa (.*)")]
    public void GivenAFilialEstaCadastradaParaAEmpresa(long branchId, long companyId)
        => _branchRepository
            .Setup(r => r.GetByIdAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Branch.Create(companyId, "Filial Centro", null, null, null, null, null, null, null, null).Value);

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

    [Given(@"o Ifood recusa a aceitacao da disputa ""(.*)"" com a mensagem ""(.*)""")]
    public void GivenOIfoodRecusaAAceitacaoDaDisputaComAMensagem(string disputeId, string message)
        => _orderClient
            .Setup(c => c.AcceptDisputeAsync(ValidToken, disputeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IfoodDisputeActionResult(false, null, message));

    [Given(@"o Ifood recusa a aceitacao da disputa ""(.*)"" sem mensagem")]
    public void GivenOIfoodRecusaAAceitacaoDaDisputaSemMensagem(string disputeId)
        => _orderClient
            .Setup(c => c.AcceptDisputeAsync(ValidToken, disputeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IfoodDisputeActionResult(false, null, null));

    [Given(@"o Ifood aceita a disputa ""(.*)"" com status ""(.*)""")]
    public void GivenOIfoodAceitaADisputaComStatus(string disputeId, string status)
        => _orderClient
            .Setup(c => c.AcceptDisputeAsync(ValidToken, disputeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IfoodDisputeActionResult(true, status, null));

    [When(@"eu tento aceitar a disputa ""(.*)"" da filial (.*)")]
    public async Task WhenEuTentoAceitarADisputaDaFilial(string disputeId, long branchId)
    {
        var handler = new AcceptIfoodDisputeCommandHandler(
            _branchRepository.Object, _tokenProvider.Object, _orderClient.Object,
            _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new AcceptIfoodDisputeCommand(branchId, disputeId), CancellationToken.None);
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

    [Then(@"a mensagem de erro deve ser ""(.*)""")]
    public void ThenAMensagemDeErroDeveSer(string message)
        => _result!.Error.Message.Should().Be(message);

    [Then(@"o status da disputa retornado deve ser ""(.*)""")]
    public void ThenOStatusDaDisputaRetornadoDeveSer(string status)
        => _result!.Value.Status.Should().Be(status);
}
