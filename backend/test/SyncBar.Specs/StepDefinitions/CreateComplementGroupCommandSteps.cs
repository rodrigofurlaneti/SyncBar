using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Catalog.Complements.CreateComplementGroup;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class CreateComplementGroupCommandSteps
{
    private readonly Mock<IComplementGroupRepository> _complementGroupRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result<long>? _result;

    [When(@"eu tento criar um grupo de complemento para a empresa (.*) com nome ""(.*)"", selecao minima (-?\d+) e selecao maxima (-?\d+)")]
    public async Task WhenEuTentoCriarUmGrupoDeComplementoParaAEmpresaComNomeSelecaoMinimaESelecaoMaxima(
        long companyId, string name, int minSelection, int maxSelection)
    {
        var handler = new CreateComplementGroupCommandHandler(
            _complementGroupRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(
            new CreateComplementGroupCommand(companyId, name, ComplementGroupTypeIds.SelecaoAdicional, minSelection, maxSelection),
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
}
