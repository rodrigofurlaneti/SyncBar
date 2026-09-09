using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Features.Auth.CustomerLogin;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;
using DomainCustomerAppUser = SyncBar.Domain.Entities.CustomerAppUser;

namespace SyncBar.Tests.Application.Features.Auth.CustomerLogin;

public sealed class CustomerLoginCommandHandlerTests
{
    private readonly ICustomerAppUserRepository _customerUserRepository = Substitute.For<ICustomerAppUserRepository>();
    private readonly ICustomerRefreshTokenRepository _refreshTokenRepository = Substitute.For<ICustomerRefreshTokenRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenProvider _jwtTokenProvider = Substitute.For<IJwtTokenProvider>();
    private readonly IAccessLogRepository _accessLogRepository = Substitute.For<IAccessLogRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CustomerLoginCommandHandler _handler;

    public CustomerLoginCommandHandlerTests()
    {
        _handler = new CustomerLoginCommandHandler(
            _customerUserRepository, _refreshTokenRepository, _passwordHasher, _jwtTokenProvider,
            _accessLogRepository, _logRepository, _unitOfWork);
    }

    private static DomainCustomerAppUser CreateActiveCustomer(
        long companyId = 1, string userName = "joao123", string email = "joao@bar.com", string passwordHash = "hashed-password")
        => DomainCustomerAppUser.Create(companyId, branchId: null, customerId: 7, userName, email, passwordHash).Value;

    private static CustomerLoginCommand MakeCommand(
        string email = "joao@bar.com", string password = "secret", int companyId = 1)
        => new(email, password, companyId, BranchId: null, IpAddress: "127.0.0.1", UserAgent: "xunit-agent");

    [Fact]
    public async Task Handle_CustomerNotFound_ShouldReturnInvalidCredentialsAndLogLoginFailed()
    {
        var command = MakeCommand();
        _customerUserRepository.GetByEmailForUpdateAsync(command.Email, command.CompanyId, Arg.Any<CancellationToken>())
            .Returns((DomainCustomerAppUser?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
        await _accessLogRepository.Received(1).AddAsync(
            Arg.Is<AccessLog>(l => l.EventType == "LoginFailed" && l.AppUserId == null && l.UserName == command.Email),
            Arg.Any<CancellationToken>());
        _passwordHasher.DidNotReceiveWithAnyArgs().Verify(default!, default!);
        await _refreshTokenRepository.DidNotReceive().AddAsync(Arg.Any<CustomerRefreshToken>(), Arg.Any<CancellationToken>());
        // Sem commit explícito nesse ramo; só resta o commit do finally da base (log de auditoria).
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CustomerInactive_ShouldReturnInvalidCredentialsAndLogLoginFailed()
    {
        var customer = CreateActiveCustomer();
        customer.Deactivate();
        var command = MakeCommand();
        _customerUserRepository.GetByEmailForUpdateAsync(command.Email, command.CompanyId, Arg.Any<CancellationToken>())
            .Returns(customer);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
        await _accessLogRepository.Received(1).AddAsync(
            Arg.Is<AccessLog>(l => l.EventType == "LoginFailed" && l.AppUserId == null),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WrongPassword_ShouldReturnInvalidCredentialsAndLogLoginFailedWithCustomerId()
    {
        var customer = CreateActiveCustomer();
        var command = MakeCommand(password: "wrong-password");
        _customerUserRepository.GetByEmailForUpdateAsync(command.Email, command.CompanyId, Arg.Any<CancellationToken>())
            .Returns(customer);
        _passwordHasher.Verify(command.Password, customer.PasswordHash).Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
        await _accessLogRepository.Received(1).AddAsync(
            Arg.Is<AccessLog>(l => l.EventType == "LoginFailed" && l.AppUserId == null && l.CustomerAppUserId == customer.Id),
            Arg.Any<CancellationToken>());
        customer.LastLoginAt.Should().BeNull();
        await _refreshTokenRepository.DidNotReceive().AddAsync(Arg.Any<CustomerRefreshToken>(), Arg.Any<CancellationToken>());
        // Commit explícito do handler no ramo de senha errada + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RefreshTokenCreationFails_ShouldReturnFailureWithoutPersistingToken()
    {
        var customer = CreateActiveCustomer();
        var command = MakeCommand();
        _customerUserRepository.GetByEmailForUpdateAsync(command.Email, command.CompanyId, Arg.Any<CancellationToken>())
            .Returns(customer);
        _passwordHasher.Verify(command.Password, customer.PasswordHash).Returns(true);
        _jwtTokenProvider.GenerateCustomerToken(customer, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<IReadOnlyCollection<string>>())
            .Returns(new AccessToken("access-token-value", DateTime.Now.AddHours(1)));
        // Token vazio faz RefreshToken.Create falhar (Error "CustomerRefreshToken.EmptyToken").
        _jwtTokenProvider.GenerateRefreshToken().Returns(string.Empty);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerRefreshToken.EmptyToken");
        customer.LastLoginAt.Should().NotBeNull();
        await _accessLogRepository.Received(1).AddAsync(
            Arg.Is<AccessLog>(l => l.EventType == "Login" && l.AppUserId == null && l.CustomerAppUserId == customer.Id),
            Arg.Any<CancellationToken>());
        await _refreshTokenRepository.DidNotReceive().AddAsync(Arg.Any<CustomerRefreshToken>(), Arg.Any<CancellationToken>());
        // O handler retorna antes do commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCredentials_ShouldReturnSuccessResponseAndPersistRefreshToken()
    {
        var customer = CreateActiveCustomer(companyId: 5, userName: "Maria", email: "maria@bar.com");
        var command = MakeCommand(email: "maria@bar.com", password: "correct-password", companyId: 5);
        _customerUserRepository.GetByEmailForUpdateAsync(command.Email, command.CompanyId, Arg.Any<CancellationToken>())
            .Returns(customer);
        _passwordHasher.Verify(command.Password, customer.PasswordHash).Returns(true);
        var accessToken = new AccessToken("access-token-value", DateTime.Now.AddHours(1));
        _jwtTokenProvider.GenerateCustomerToken(customer, Arg.Is<IReadOnlyCollection<string>>(r => r.Contains("Customer")), Arg.Any<IReadOnlyCollection<string>>())
            .Returns(accessToken);
        _jwtTokenProvider.GenerateRefreshToken().Returns("new-refresh-token-value");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token-value");
        result.Value.ExpiresAt.Should().Be(accessToken.ExpiresAt);
        result.Value.RefreshToken.Should().Be("new-refresh-token-value");
        result.Value.UserName.Should().Be(customer.UserName);
        result.Value.CustomerId.Should().Be(customer.CustomerId!.Value);
        result.Value.CompanyId.Should().Be(customer.CompanyId);
        result.Value.RefreshTokenExpiresAt.Should().BeCloseTo(DateTime.Now.AddDays(7), TimeSpan.FromMinutes(1));

        customer.LastLoginAt.Should().NotBeNull();
        customer.FailedAccessCount.Should().Be(0);
        await _accessLogRepository.Received(1).AddAsync(
            Arg.Is<AccessLog>(l => l.EventType == "Login" && l.AppUserId == null && l.CustomerAppUserId == customer.Id),
            Arg.Any<CancellationToken>());
        await _refreshTokenRepository.Received(1).AddAsync(
            Arg.Is<CustomerRefreshToken>(rt => rt.Token == "new-refresh-token-value" && rt.CustomerAppUserId == customer.Id),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo de sucesso + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
