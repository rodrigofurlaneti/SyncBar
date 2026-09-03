using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Features.Auth;
using SyncBar.Application.Features.Auth.Login;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Login de usuario")]
public sealed class LoginCommandSteps
{
    private readonly Mock<IAppUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenProvider> _jwtTokenProvider = new();
    private readonly Mock<IAccessLogRepository> _accessLogRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private AppUser? _user;
    private Result<LoginResponse>? _result;

    [Given(@"nao existe nenhum usuario com o nome de usuario (.*)")]
    public void GivenNaoExisteNenhumUsuarioComONomeDeUsuario(string userName)
        => _userRepository
            .Setup(r => r.GetByUserNameForUpdateAsync(userName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);

    [Given(@"existe um usuario ativo (.*)")]
    public void GivenExisteUmUsuarioAtivo(string userName)
    {
        _user = AppUser.Create(1, null, userName, $"{userName}@bar.com", "hashed-password").Value;
        _userRepository
            .Setup(r => r.GetByUserNameForUpdateAsync(userName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_user);
    }

    [Given(@"existe um usuario inativo (.*)")]
    public void GivenExisteUmUsuarioInativo(string userName)
    {
        _user = AppUser.Create(1, null, userName, $"{userName}@bar.com", "hashed-password").Value;
        _user.Deactivate();
        _userRepository
            .Setup(r => r.GetByUserNameForUpdateAsync(userName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_user);
    }

    [Given(@"o usuario (.*) esta bloqueado por excesso de tentativas de login")]
    public void GivenOUsuarioEstaBloqueadoPorExcessoDeTentativasDeLogin(string userName)
    {
        _user = AppUser.Create(1, null, userName, $"{userName}@bar.com", "hashed-password").Value;
        for (var i = 0; i < 5; i++)
            _user.RegisterLoginFailure();

        _userRepository
            .Setup(r => r.GetByUserNameForUpdateAsync(userName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_user);
    }

    [Given(@"a senha informada esta correta")]
    public void GivenASenhaInformadaEstaCorreta()
    {
        _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), _user!.PasswordHash)).Returns(true);
        _userRepository.Setup(r => r.GetRoleNamesAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<string>());
        _userRepository.Setup(r => r.GetPermissionCodesAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<string>());
        _jwtTokenProvider
            .Setup(p => p.GenerateToken(_user, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<IReadOnlyCollection<string>>()))
            .Returns(new AccessToken("access-token-value", DateTime.Now.AddHours(1)));
        _jwtTokenProvider.Setup(p => p.GenerateRefreshToken()).Returns("refresh-token-value");
    }

    [Given(@"a senha informada esta incorreta")]
    public void GivenASenhaInformadaEstaIncorreta()
        => _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), _user!.PasswordHash)).Returns(false);

    [Given(@"o provedor de token gera um refresh token vazio")]
    public void GivenOProvedorDeTokenGeraUmRefreshTokenVazio()
        => _jwtTokenProvider.Setup(p => p.GenerateRefreshToken()).Returns(string.Empty);

    [When(@"eu tento fazer login com o usuario (.*) e a senha (.*)")]
    public async Task WhenEuTentoFazerLoginComOUsuarioEASenha(string userName, string password)
    {
        var handler = new LoginCommandHandler(
            _userRepository.Object, _refreshTokenRepository.Object, _passwordHasher.Object,
            _jwtTokenProvider.Object, _accessLogRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new LoginCommand(userName, password), CancellationToken.None);
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

    [Then(@"o numero de tentativas de login falhas do usuario deve ser (.*)")]
    public void ThenONumeroDeTentativasDeLoginFalhasDoUsuarioDeveSer(int count)
        => _user!.FailedAccessCount.Should().Be(count);
}
