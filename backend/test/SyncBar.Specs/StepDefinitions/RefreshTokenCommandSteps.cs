using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Features.Auth;
using SyncBar.Application.Features.Auth.Refresh;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Renovar token de acesso")]
public sealed class RefreshTokenCommandSteps
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IAppUserRepository> _userRepository = new();
    private readonly Mock<IJwtTokenProvider> _jwtTokenProvider = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RefreshToken? _storedToken;
    private Result<LoginResponse>? _result;

    [Given(@"nao existe nenhum refresh token com o valor (.*)")]
    public void GivenNaoExisteNenhumRefreshTokenComOValor(string tokenValue)
        => _refreshTokenRepository
            .Setup(r => r.GetByTokenForUpdateAsync(tokenValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

    [Given(@"existe um refresh token valido (.*) do usuario (.*)")]
    public void GivenExisteUmRefreshTokenValidoDoUsuario(string tokenValue, long userId)
    {
        _storedToken = RefreshToken.Create(userId, tokenValue, DateTime.Now.AddDays(3)).Value;
        _refreshTokenRepository
            .Setup(r => r.GetByTokenForUpdateAsync(tokenValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_storedToken);
    }

    [Given(@"o token (.*) ja foi revogado")]
    public void GivenOTokenJaFoiRevogado(string tokenValue)
        => _storedToken!.Revoke();

    [Given(@"nao existe usuario com o id (.*)")]
    public void GivenNaoExisteUsuarioComOId(long userId)
        => _userRepository
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);

    [Given(@"o usuario (.*) esta inativo")]
    public void GivenOUsuarioEstaInativo(long userId)
    {
        var user = AppUser.Create(1, null, "joao", "joao@bar.com", "hashed-password").Value;
        user.Deactivate();
        _userRepository
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
    }

    [Given(@"o usuario (.*) esta ativo")]
    public void GivenOUsuarioEstaAtivo(long userId)
    {
        var user = AppUser.Create(1, null, "joao", "joao@bar.com", "hashed-password").Value;
        _userRepository
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepository.Setup(r => r.GetRoleNamesAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<string>());
        _userRepository.Setup(r => r.GetPermissionCodesAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<string>());
        _jwtTokenProvider
            .Setup(p => p.GenerateToken(user, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<IReadOnlyCollection<string>>()))
            .Returns(new AccessToken("access-token-value", DateTime.Now.AddHours(1)));
        _jwtTokenProvider.Setup(p => p.GenerateRefreshToken()).Returns("new-refresh-token-value");
    }

    [Given(@"o provedor de token gera um novo refresh token vazio")]
    public void GivenOProvedorDeTokenGeraUmNovoRefreshTokenVazio()
        => _jwtTokenProvider.Setup(p => p.GenerateRefreshToken()).Returns(string.Empty);

    [When(@"eu tento renovar o token (.*)")]
    public async Task WhenEuTentoRenovarOToken(string tokenValue)
    {
        var handler = new RefreshTokenCommandHandler(
            _refreshTokenRepository.Object, _userRepository.Object, _jwtTokenProvider.Object,
            _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new RefreshTokenCommand(tokenValue), CancellationToken.None);
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

    [Then(@"o token antigo deve estar revogado")]
    public void ThenOTokenAntigoDeveEstarRevogado()
        => _storedToken!.RevokedAt.Should().NotBeNull();

    [Then(@"o token antigo nao deve estar revogado")]
    public void ThenOTokenAntigoNaoDeveEstarRevogado()
        => _storedToken!.RevokedAt.Should().BeNull();
}
