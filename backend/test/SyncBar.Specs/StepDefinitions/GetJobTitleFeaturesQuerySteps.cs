using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Access.GetJobTitleFeatures;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class GetJobTitleFeaturesQuerySteps
{
    private readonly Mock<IJobTitleFeatureRepository> _jobTitleFeatureRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly List<JobTitleFeature> _links = new();
    private Result<IReadOnlyCollection<long>>? _result;

    [Given(@"o cargo (.*) nao tem features vinculadas")]
    public void GivenOCargoNaoTemFeaturesVinculadas(long jobTitleId)
        => _jobTitleFeatureRepository
            .Setup(r => r.GetByJobTitleAsync(jobTitleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<JobTitleFeature>());

    [Given(@"o cargo (.*) tem a feature (.*) vinculada e ativa")]
    public void GivenOCargoTemAFeatureVinculadaEAtiva(long jobTitleId, long featureId)
    {
        _links.Add(JobTitleFeature.Create(jobTitleId, featureId).Value);
        _jobTitleFeatureRepository
            .Setup(r => r.GetByJobTitleAsync(jobTitleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_links.ToArray());
    }

    [Given(@"o cargo (.*) tem a feature (.*) vinculada mas desativada")]
    public void GivenOCargoTemAFeatureVinculadaMasDesativada(long jobTitleId, long featureId)
    {
        var link = JobTitleFeature.Create(jobTitleId, featureId).Value;
        link.Deactivate();
        _links.Add(link);
        _jobTitleFeatureRepository
            .Setup(r => r.GetByJobTitleAsync(jobTitleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_links.ToArray());
    }

    [When(@"eu busco as features vinculadas ao cargo (.*)")]
    public async Task WhenEuBuscoAsFeaturesVinculadasAoCargo(long jobTitleId)
    {
        var handler = new GetJobTitleFeaturesQueryHandler(_jobTitleFeatureRepository.Object, _logRepository.Object, _unitOfWork.Object);
        _result = await handler.Handle(new GetJobTitleFeaturesQuery(jobTitleId), CancellationToken.None);
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
