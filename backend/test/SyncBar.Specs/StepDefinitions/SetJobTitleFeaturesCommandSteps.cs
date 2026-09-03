using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Access.SetJobTitleFeatures;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Definir features de um cargo")]
public sealed class SetJobTitleFeaturesCommandSteps
{
    private readonly Mock<IJobTitleRepository> _jobTitleRepository = new();
    private readonly Mock<IJobTitleFeatureRepository> _jobTitleFeatureRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly List<JobTitleFeature> _links = new();
    private readonly Dictionary<long, JobTitleFeature> _linksByFeatureId = new();
    private Result<Result>? _result;

    [Given(@"nao existe o cargo (.*)")]
    public void GivenNaoExisteOCargo(long jobTitleId)
        => _jobTitleRepository
            .Setup(r => r.GetByIdAsync(jobTitleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobTitle?)null);

    [Given(@"existe o cargo (.*) ativo")]
    public void GivenExisteOCargoAtivo(long jobTitleId)
        => _jobTitleRepository
            .Setup(r => r.GetByIdAsync(jobTitleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(JobTitle.Create(1, "Garcom").Value);

    [Given(@"existe o cargo (.*) inativo")]
    public void GivenExisteOCargoInativo(long jobTitleId)
    {
        var jobTitle = JobTitle.Create(1, "Garcom").Value;
        jobTitle.Deactivate();
        _jobTitleRepository
            .Setup(r => r.GetByIdAsync(jobTitleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobTitle);
    }

    [Given(@"o cargo (.*) tem a feature (.*) vinculada e ativa")]
    public void GivenOCargoTemAFeatureVinculadaEAtiva(long jobTitleId, long featureId)
    {
        var link = JobTitleFeature.Create(jobTitleId, featureId).Value;
        _links.Add(link);
        _linksByFeatureId[featureId] = link;
        _jobTitleFeatureRepository
            .Setup(r => r.GetByJobTitleForUpdateAsync(jobTitleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_links.ToArray());
    }

    [Given(@"o cargo (.*) tem a feature (.*) vinculada mas desativada")]
    public void GivenOCargoTemAFeatureVinculadaMasDesativada(long jobTitleId, long featureId)
    {
        var link = JobTitleFeature.Create(jobTitleId, featureId).Value;
        link.Deactivate();
        _links.Add(link);
        _linksByFeatureId[featureId] = link;
        _jobTitleFeatureRepository
            .Setup(r => r.GetByJobTitleForUpdateAsync(jobTitleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_links.ToArray());
    }

    [Given(@"o cargo (.*) nao tem vinculos existentes")]
    public void GivenOCargoNaoTemVinculosExistentes(long jobTitleId)
        => _jobTitleFeatureRepository
            .Setup(r => r.GetByJobTitleForUpdateAsync(jobTitleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<JobTitleFeature>());

    [When(@"eu defino as features (.*) para o cargo (.*)")]
    public async Task WhenEuDefinoAsFeaturesParaOCargo(string featureIds, long jobTitleId)
    {
        var ids = featureIds.Split(',', StringSplitOptions.TrimEntries).Select(long.Parse).ToList();
        var handler = new SetJobTitleFeaturesCommandHandler(
            _jobTitleRepository.Object, _jobTitleFeatureRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new SetJobTitleFeaturesCommand(jobTitleId, ids), CancellationToken.None);
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

    [Then(@"o vinculo da feature (.*) do cargo deve estar ativo")]
    public void ThenOVinculoDaFeatureDoCargoDeveEstarAtivo(long featureId)
        => _linksByFeatureId[featureId].IsActive.Should().BeTrue();

    [Then(@"o vinculo da feature (.*) do cargo deve estar inativo")]
    public void ThenOVinculoDaFeatureDoCargoDeveEstarInativo(long featureId)
        => _linksByFeatureId[featureId].IsActive.Should().BeFalse();

    [Then(@"deve ser criado um novo vinculo para a feature (.*) do cargo")]
    public void ThenDeveSerCriadoUmNovoVinculoParaAFeatureDoCargo(long featureId)
        => _jobTitleFeatureRepository.Verify(
            r => r.AddAsync(It.Is<JobTitleFeature>(l => l.AppFeatureId == featureId), It.IsAny<CancellationToken>()),
            Times.Once);

    [Then(@"nenhum novo vinculo do cargo deve ser criado")]
    public void ThenNenhumNovoVinculoDoCargoDeveSerCriado()
        => _jobTitleFeatureRepository.Verify(
            r => r.AddAsync(It.IsAny<JobTitleFeature>(), It.IsAny<CancellationToken>()),
            Times.Never);
}
