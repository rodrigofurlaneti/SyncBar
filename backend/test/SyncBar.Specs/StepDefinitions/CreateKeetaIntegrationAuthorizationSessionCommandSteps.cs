using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Criar sessao de autorizacao Keeta")]
public sealed class CreateKeetaIntegrationAuthorizationSessionCommandSteps
{
    private readonly Mock<IKeetaIntegrationAuthorizationSessionRepository> _sessionRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result<CreateKeetaIntegrationAuthorizationSessionResponse>? _result;

    [Given(@"ja existe uma sessao de autorizacao Keeta com o authId ""(.*)"" para a empresa (.*) filial (.*)")]
    public void GivenJaExisteUmaSessaoComOAuthIdParaAEmpresaFilial(string authId, long companyId, long branchId)
    {
        var session = KeetaIntegrationAuthorizationSession.Create(companyId, branchId, authId, 1).Value;
        _sessionRepository
            .Setup(r => r.GetByAuthIdAsync(authId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
    }

    [Given(@"nao existe sessao de autorizacao Keeta com o authId ""(.*)""")]
    public void GivenNaoExisteSessaoComOAuthId(string authId)
        => _sessionRepository
            .Setup(r => r.GetByAuthIdAsync(authId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KeetaIntegrationAuthorizationSession?)null);

    [When(@"eu tento criar uma sessao de autorizacao Keeta para a empresa (.*) filial (.*) com authId ""(.*)"" e tipo de operacao (.*)")]
    public async Task WhenEuTentoCriarUmaSessaoParaAEmpresaFilialComAuthIdETipoDeOperacao(
        long companyId, long branchId, string authId, int operationType)
    {
        var handler = new CreateKeetaIntegrationAuthorizationSessionCommandHandler(
            _sessionRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(
            new CreateKeetaIntegrationAuthorizationSessionCommand(companyId, branchId, authId, operationType),
            CancellationToken.None);
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

    [Then(@"a sessao de autorizacao Keeta criada deve ter o authId ""(.*)"" e tipo de operacao (.*)")]
    public void ThenASessaoCriadaDeveTerOAuthIdETipoDeOperacao(string authId, int operationType)
    {
        _result!.Value.AuthId.Should().Be(authId);
        _result.Value.OperationType.Should().Be(operationType);
    }
}
