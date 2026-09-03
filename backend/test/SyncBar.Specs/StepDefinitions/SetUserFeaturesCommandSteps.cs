using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Access.SetUserFeatures;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Definir features de um usuario")]
public sealed class SetUserFeaturesCommandSteps
{
    private readonly Mock<IAppUserRepository> _userRepository = new();
    private readonly Mock<IAppUserFeatureRepository> _userFeatureRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly List<AppUserFeature> _links = new();
    private readonly Dictionary<long, AppUserFeature> _linksByFeatureId = new();
    private Result? _result;

    [Given(@"nao existe o usuario (.*)")]
    public void GivenNaoExisteOUsuario(long appUserId)
        => _userRepository
            .Setup(r => r.GetByIdAsync(appUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);

    [Given(@"existe o usuario (.*) ativo")]
    public void GivenExisteOUsuarioAtivo(long appUserId)
        => _userRepository
            .Setup(r => r.GetByIdAsync(appUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AppUser.Create(1, null, "joao", "joao@bar.com", "hashed-password").Value);

    [Given(@"existe o usuario (.*) inativo")]
    public void GivenExisteOUsuarioInativo(long appUserId)
    {
        var user = AppUser.Create(1, null, "joao", "joao@bar.com", "hashed-password").Value;
        user.Deactivate();
        _userRepository
            .Setup(r => r.GetByIdAsync(appUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
    }

    [Given(@"o usuario (.*) tem a feature (.*) vinculada e ativa")]
    public void GivenOUsuarioTemAFeatureVinculadaEAtiva(long appUserId, long featureId)
    {
        var link = AppUserFeature.Create(appUserId, featureId).Value;
        _links.Add(link);
        _linksByFeatureId[featureId] = link;
        _userFeatureRepository
            .Setup(r => r.GetByUserForUpdateAsync(appUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_links.ToArray());
    }

    [Given(@"o usuario (.*) tem a feature (.*) vinculada mas desativada")]
    public void GivenOUsuarioTemAFeatureVinculadaMasDesativada(long appUserId, long featureId)
    {
        var link = AppUserFeature.Create(appUserId, featureId).Value;
        link.Deactivate();
        _links.Add(link);
        _linksByFeatureId[featureId] = link;
        _userFeatureRepository
            .Setup(r => r.GetByUserForUpdateAsync(appUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_links.ToArray());
    }

    [Given(@"o usuario (.*) nao tem vinculos existentes")]
    public void GivenOUsuarioNaoTemVinculosExistentes(long appUserId)
        => _userFeatureRepository
            .Setup(r => r.GetByUserForUpdateAsync(appUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AppUserFeature>());

    [When(@"eu defino as features (.*) para o usuario (.*)")]
    public async Task WhenEuDefinoAsFeaturesParaOUsuario(string featureIds, long appUserId)
    {
        var ids = featureIds.Split(',', StringSplitOptions.TrimEntries).Select(long.Parse).ToList();
        var handler = new SetUserFeaturesCommandHandler(_userRepository.Object, _userFeatureRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new SetUserFeaturesCommand(appUserId, ids), CancellationToken.None);
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

    [Then(@"o vinculo da feature (.*) do usuario deve estar ativo")]
    public void ThenOVinculoDaFeatureDoUsuarioDeveEstarAtivo(long featureId)
        => _linksByFeatureId[featureId].IsActive.Should().BeTrue();

    [Then(@"o vinculo da feature (.*) do usuario deve estar inativo")]
    public void ThenOVinculoDaFeatureDoUsuarioDeveEstarInativo(long featureId)
        => _linksByFeatureId[featureId].IsActive.Should().BeFalse();

    [Then(@"deve ser criado um novo vinculo para a feature (.*) do usuario")]
    public void ThenDeveSerCriadoUmNovoVinculoParaAFeatureDoUsuario(long featureId)
        => _userFeatureRepository.Verify(
            r => r.AddAsync(It.Is<AppUserFeature>(l => l.AppFeatureId == featureId), It.IsAny<CancellationToken>()),
            Times.Once);

    [Then(@"nenhum novo vinculo do usuario deve ser criado")]
    public void ThenNenhumNovoVinculoDoUsuarioDeveSerCriado()
        => _userFeatureRepository.Verify(
            r => r.AddAsync(It.IsAny<AppUserFeature>(), It.IsAny<CancellationToken>()),
            Times.Never);
}
