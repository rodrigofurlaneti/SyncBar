using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Features.Companies.Register;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class RegisterCompanyCommandSteps
{
    private readonly Mock<ICompanyRepository> _companyRepository = new();
    private readonly Mock<IBranchRepository> _branchRepository = new();
    private readonly Mock<IRoleRepository> _roleRepository = new();
    private readonly Mock<IAppUserRepository> _userRepository = new();
    private readonly Mock<IUserRoleRepository> _userRoleRepository = new();
    private readonly Mock<IDiningTableRepository> _diningTableRepository = new();
    private readonly Mock<IComandaRepository> _comandaRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IJobTitleRepository> _jobTitleRepository = new();
    private readonly Mock<IEmployeeRepository> _employeeRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result<RegisterCompanyResponse>? _result;

    [Given(@"ja existe uma empresa cadastrada com o mesmo cnpj do onboarding")]
    public void GivenJaExisteUmaEmpresaCadastradaComOMesmoCnpjDoOnboarding()
        => _companyRepository
            .Setup(r => r.ExistsByCnpjAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

    [Given(@"o nome de usuario ou email do administrador do onboarding ja esta em uso")]
    public void GivenONomeDeUsuarioOuEmailDoAdministradorDoOnboardingJaEstaEmUso()
        => _userRepository
            .Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

    [Given(@"ja existe um funcionario cadastrado com o cpf do administrador do onboarding")]
    public void GivenJaExisteUmFuncionarioCadastradoComOCpfDoAdministradorDoOnboarding()
        => _employeeRepository
            .Setup(r => r.ExistsByCpfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

    [Given(@"os dados do onboarding ainda nao estao cadastrados no sistema")]
    public void GivenOsDadosDoOnboardingAindaNaoEstaoCadastradosNoSistema()
        => _passwordHasher
            .Setup(h => h.Hash(It.IsAny<string>()))
            .Returns("hashed-password");

    [When(@"eu registro a nova empresa no onboarding")]
    public async Task WhenEuRegistroANovaEmpresaNoOnboarding()
    {
        var handler = new RegisterCompanyCommandHandler(
            _companyRepository.Object, _branchRepository.Object, _roleRepository.Object, _userRepository.Object,
            _userRoleRepository.Object, _diningTableRepository.Object, _comandaRepository.Object,
            _categoryRepository.Object, _jobTitleRepository.Object, _employeeRepository.Object,
            _passwordHasher.Object, _logRepository.Object, _unitOfWork.Object);

        var command = new RegisterCompanyCommand(
            "Bar do Ze Ltda", "Bar do Ze", "12345678000100", "contato@bardoze.com.br", "11999990000",
            "Filial Centro", null, "Rua das Flores", "100", "Centro", "Sao Paulo", "SP", "01000000",
            "Jose da Silva", "12345678901", "jose.silva", "jose.silva@bardoze.com.br", "SenhaForte123");

        _result = await handler.Handle(command, CancellationToken.None);
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

    [Then(@"a empresa, a filial e o usuario administrador devem ser criados")]
    public void ThenAEmpresaAFilialEOUsuarioAdministradorDevemSerCriados()
    {
        _companyRepository.Verify(r => r.AddAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()), Times.Once);
        _branchRepository.Verify(r => r.AddAsync(It.IsAny<Branch>(), It.IsAny<CancellationToken>()), Times.Once);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Then(@"as (.*) categorias, mesas e comandas padrao devem ser criadas")]
    public void ThenAsCategoriasMesasEComandasPadraoDevemSerCriadas(int count)
    {
        _categoryRepository.Verify(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Exactly(count));
        _diningTableRepository.Verify(r => r.AddAsync(It.IsAny<DiningTable>(), It.IsAny<CancellationToken>()), Times.Exactly(count));
        _comandaRepository.Verify(r => r.AddAsync(It.IsAny<Comanda>(), It.IsAny<CancellationToken>()), Times.Exactly(count));
    }

    [Then(@"o usuario administrador deve ser vinculado ao perfil de administrador criado")]
    public void ThenOUsuarioAdministradorDeveSerVinculadoAoPerfilDeAdministradorCriado()
        => _userRoleRepository.Verify(r => r.AddAsync(It.IsAny<UserRole>(), It.IsAny<CancellationToken>()), Times.Once);
}
