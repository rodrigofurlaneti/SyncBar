using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Access.GetUserFeatures;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class GetUserFeaturesQuerySteps
{
    private readonly Mock<IAppUserFeatureRepository> _userFeatureRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly List<AppUserFeature> _links = new();
    private Result<IReadOnlyCollection<long>>? _result;

    [Given(@"o usuario (.*) nao tem features vinculadas diretamente")]
    public void GivenOUsuarioNaoTemFeaturesVinculadasDiretamente(long appUserId)
        => _userFeatureRepository
            .Setup(r => r.GetByUserAsync(appUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AppUserFeature>());

    [Given(@"o usuario (.*) tem a feature (.*) vinculada e ativa")]
    public void GivenOUsuarioTemAFeatureVinculadaEAtiva(long appUserId, long featureId)
    {
        _links.Add(AppUserFeature.Create(appUserId, featureId).Value);
        _userFeatureRepository
            .Setup(r => r.GetByUserAsync(appUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_links.ToArray());
    }

    [Given(@"o usuario (.*) tem a feature (.*) vinculada mas desativada")]
    public void GivenOUsuarioTemAFeatureVinculadaMasDesativada(long appUserId, long featureId)
    {
        var link = AppUserFeature.Create(appUserId, featureId).Value;
        link.Deactivate();
        _links.Add(link);
        _userFeatureRepository
            .Setup(r => r.GetByUserAsync(appUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_links.ToArray());
    }

    [When(@"eu busco as features vinculadas ao usuario (.*)")]
    public async Task WhenEuBuscoAsFeaturesVinculadasAoUsuario(long appUserId)
    {
        var handler = new GetUserFeaturesQueryHandler(_userFeatureRepository.Object, _logRepository.Object, _unitOfWork.Object);
        _result = await handler.Handle(new GetUserFeaturesQuery(appUserId), CancellationToken.None);
    }

    [Then(@"a operacao deve ter sucesso")]
    public void ThenAOperacaoDeveTerSucesso()
        => _result!.IsSuccess.Should().BeTrue();

    [Then(@"a lista de ids de features deve estar vazia")]
    public void ThenAListaDeIdsDeFeaturesDeveEstarVazia()
        => _result!.Value.Should().BeEmpty();

    [Then(@"a lista de ids de features deve ser (.*)")]
    public void ThenAListaDeIdsDeFeaturesDeveSer(string ids)
    {
        var expected = ids.Split(',', StringSplitOptions.TrimEntries).Select(long.Parse).ToArray();
        _result!.Value.Should().Equal(expected);
    }
}
