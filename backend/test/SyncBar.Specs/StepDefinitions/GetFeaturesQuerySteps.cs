using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Access;
using SyncBar.Application.Features.Access.GetFeatures;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class GetFeaturesQuerySteps
{
    private readonly Mock<IAppFeatureRepository> _featureRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly List<AppFeature> _features = new();
    private Result<IReadOnlyCollection<FeatureResponse>>? _result;

    [Given(@"nao existem features cadastradas")]
    public void GivenNaoExistemFeaturesCadastradas()
        => _featureRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AppFeature>());

    [Given(@"existe a feature ""(.*)"" com o nome ""(.*)""")]
    public void GivenExisteAFeatureComONome(string code, string name)
    {
        _features.Add(AppFeature.Create(code, name).Value);
        _featureRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_features.ToArray());
    }

    [When(@"eu busco a lista de features")]
    public async Task WhenEuBuscoAListaDeFeatures()
    {
        var handler = new GetFeaturesQueryHandler(_featureRepository.Object, _logRepository.Object, _unitOfWork.Object);
        _result = await handler.Handle(new GetFeaturesQuery(), CancellationToken.None);
    }

    [Then(@"a operacao deve ter sucesso")]
    public void ThenAOperacaoDeveTerSucesso()
        => _result!.IsSuccess.Should().BeTrue();

    [Then(@"a lista de features deve estar vazia")]
    public void ThenAListaDeFeaturesDeveEstarVazia()
        => _result!.Value.Should().BeEmpty();

    [Then(@"a lista de features deve conter o codigo ""(.*)""")]
    public void ThenAListaDeFeaturesDeveConterOCodigo(string code)
        => _result!.Value.Should().Contain(f => f.Code == code);
}
