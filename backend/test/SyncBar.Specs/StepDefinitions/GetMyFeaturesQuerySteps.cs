using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Access;
using SyncBar.Application.Features.Access.GetMyFeatures;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

// Observacao: AppFeature.Id nao e controlavel em teste (sem setter publico, sempre 0 ate ser
// persistido pelo EF), enquanto JobTitleFeature/AppUserFeature exigem AppFeatureId > 0. Por isso
// os cenarios aqui nao tentam validar a correspondencia exata entre vinculos com id explicito e
// as features cadastradas (essa combinacao so e observavel em teste de integracao); cobrem apenas
// os ramos que independem dessa limitacao.
[Binding]
public sealed class GetMyFeaturesQuerySteps
{
    private readonly Mock<IAppUserRepository> _userRepository = new();
    private readonly Mock<IEmployeeRepository> _employeeRepository = new();
    private readonly Mock<IAppFeatureRepository> _featureRepository = new();
    private readonly Mock<IJobTitleFeatureRepository> _jobTitleFeatureRepository = new();
    private readonly Mock<IAppUserFeatureRepository> _userFeatureRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly List<AppFeature> _allFeatures = new();
    private Result<MyFeaturesResponse>? _result;

    [Given(@"existe a feature cadastrada ""(.*)""")]
    public void GivenExisteAFeatureCadastrada(string code)
    {
        _allFeatures.Add(AppFeature.Create(code, code).Value);
        _featureRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_allFeatures.ToArray());
    }

    [Given(@"nao existe nenhum usuario com o id (.*)")]
    public void GivenNaoExisteNenhumUsuarioComOId(long appUserId)
        => _userRepository
            .Setup(r => r.GetByIdAsync(appUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);

    [Given(@"o usuario (.*) esta inativo e sem cargo")]
    public void GivenOUsuarioEstaInativoESemCargo(long appUserId)
    {
        var user = AppUser.Create(1, null, "joao", "joao@bar.com", "hashed-password").Value;
        user.Deactivate();
        _userRepository
            .Setup(r => r.GetByIdAsync(appUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
    }

    [Given(@"o usuario (.*) esta ativo sem cargo e sem features vinculadas diretamente")]
    public void GivenOUsuarioEstaAtivoSemCargoESemFeaturesVinculadasDiretamente(long appUserId)
    {
        var user = AppUser.Create(1, null, "joao", "joao@bar.com", "hashed-password").Value;
        _userRepository
            .Setup(r => r.GetByIdAsync(appUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userFeatureRepository
            .Setup(r => r.GetByUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AppUserFeature>());
        _featureRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_allFeatures.ToArray());
    }

    [When(@"eu busco minhas features como gerente do usuario (.*)")]
    public async Task WhenEuBuscoMinhasFeaturesComoGerenteDoUsuario(long appUserId)
        => _result = await CreateHandler().Handle(new GetMyFeaturesQuery(appUserId, true), CancellationToken.None);

    [When(@"eu busco minhas features do usuario (.*)")]
    public async Task WhenEuBuscoMinhasFeaturesDoUsuario(long appUserId)
        => _result = await CreateHandler().Handle(new GetMyFeaturesQuery(appUserId, false), CancellationToken.None);

    private GetMyFeaturesQueryHandler CreateHandler() => new(
        _userRepository.Object, _employeeRepository.Object, _featureRepository.Object,
        _jobTitleFeatureRepository.Object, _userFeatureRepository.Object, _logRepository.Object, _unitOfWork.Object);

    [Then(@"a operacao deve falhar com o erro ""(.*)""")]
    public void ThenAOperacaoDeveFalharComOErro(string errorCode)
    {
        _result!.IsFailure.Should().BeTrue();
        _result.Error.Code.Should().Be(errorCode);
    }

    [Then(@"a operacao deve ter sucesso")]
    public void ThenAOperacaoDeveTerSucesso()
        => _result!.IsSuccess.Should().BeTrue();

    [Then(@"eu devo poder gerenciar o acesso")]
    public void ThenEuDevoPoderGerenciarOAcesso()
        => _result!.Value.CanManageAccess.Should().BeTrue();

    [Then(@"eu nao devo poder gerenciar o acesso")]
    public void ThenEuNaoDevoPoderGerenciarOAcesso()
        => _result!.Value.CanManageAccess.Should().BeFalse();

    [Then(@"a lista das minhas features deve conter o codigo ""(.*)""")]
    public void ThenAListaDasMinhasFeaturesDeveConterOCodigo(string code)
        => _result!.Value.Features.Should().Contain(code);

    [Then(@"a lista das minhas features deve estar vazia")]
    public void ThenAListaDasMinhasFeaturesDeveEstarVazia()
        => _result!.Value.Features.Should().BeEmpty();
}
